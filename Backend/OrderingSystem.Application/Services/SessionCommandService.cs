using Microsoft.Extensions.Caching.Memory;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Bills;
using OrderingSystem.Application.Interfaces.Data;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Services
{
    public class SessionCommandService : ISessionCommandService
    {
        private readonly ITableSessionRepository _tableSessionRepository;
        private readonly IDeviceSessionRepository _deviceSessionRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ITaxRepository _taxRepository;
        private readonly ITaxCalculationService _taxCalculationService;
        private readonly IBillRepository _billRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeNotifier _notifier;
        private readonly IMemoryCache _cache;

        public SessionCommandService(
            ITableSessionRepository tableSessionRepository,
            IDeviceSessionRepository deviceSessionRepository,
            ITableRepository tableRepository,
            IOrderRepository orderRepository,
            ITaxRepository taxRepository,
            ITaxCalculationService taxCalculationService,
            IBillRepository billRepository,
            IUnitOfWork unitOfWork,
            IRealTimeNotifier notifier,
            IMemoryCache cache)
        {
            _tableSessionRepository = tableSessionRepository;
            _deviceSessionRepository = deviceSessionRepository;
            _tableRepository = tableRepository;
            _orderRepository = orderRepository;
            _taxRepository = taxRepository;
            _taxCalculationService = taxCalculationService;
            _billRepository = billRepository;
            _unitOfWork = unitOfWork;
            _notifier = notifier;
            _cache = cache;
        }

        public async Task<Result<SessionResponse>> ProcessTableQrCodeAsync(string qrCode, Guid? deviceSessionId = null)
        {
            // 1. Cross-Table Interception: Lock the user into their existing active session
            if (deviceSessionId.HasValue)
            {
                var existingDevice = await _deviceSessionRepository.GetDeviceSessionByIdAsync(deviceSessionId.Value);

                if (existingDevice != null && existingDevice.TableSession.Status != enSessionStatus.Closed)
                {
                    // Return their existing session, ignoring the new QR code entirely
                    return Result<SessionResponse>.Success(existingDevice.TableSession.ToResponse(existingDevice));
                }
            }

            var table = await _tableSessionRepository.GetTableWithActiveSessionAsync(qrCode);

            if (table == null)
            {
                return Result<SessionResponse>.Failure("Table was not found.", enErrorType.NotFound);
            }

            if (table.Status == enTableStatus.Billing)
            {
                var billingSession = table.Sessions.FirstOrDefault(s => s.ClosedAt == null);
                if (billingSession != null && deviceSessionId.HasValue && billingSession.Devices.Any(d => d.DeviceSessionId == deviceSessionId.Value))
                {
                    return AccessTableSessionAsync(billingSession, deviceSessionId.Value);
                }

                return Result<SessionResponse>.Failure("This table is currently processing payment. Please wait until it is cleared.", enErrorType.Conflict);
            }

            var activeSession = table.Sessions.FirstOrDefault(s => s.ClosedAt == null && s.Status != enSessionStatus.Closed);

            if (activeSession != null)
            {
                if (deviceSessionId.HasValue && activeSession.Devices.Any(d => d.DeviceSessionId == deviceSessionId.Value))
                {
                    return AccessTableSessionAsync(activeSession, deviceSessionId.Value);
                }

                return await JoinTableSessionAsync(activeSession, Guid.CreateVersion7());
            }

            try
            {
                Result<SessionResponse>? activationResult = null;

                // --- Transaction Wrapper Added ---
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    table.Status = enTableStatus.Occupied;
                    await _tableRepository.UpdateTableAsync(table);
                    //activationResult = await ActivateTableSessionAsync(table.TableId, deviceSessionId ?? Guid.CreateVersion7());
                    activationResult = await ActivateTableSessionAsync(table.TableId, Guid.CreateVersion7());
                });

                return activationResult!;
            }
            catch (Exception)
            {
                var refreshedTable = await _tableSessionRepository.GetTableWithActiveSessionAsync(qrCode);
                var newActiveSession = refreshedTable?.Sessions.FirstOrDefault(s => s.ClosedAt == null && s.Status != enSessionStatus.Closed);

                if (newActiveSession != null)
                {
                    return await JoinTableSessionAsync(newActiveSession, deviceSessionId ?? Guid.CreateVersion7());
                }

                throw;
            }
        }

        private async Task<Result<SessionResponse>> ActivateTableSessionAsync(int tableId, Guid deviceSessionId)
        {
            var tableSession = new TableSession
            {
                TableSessionId = Guid.CreateVersion7(),
                TableId = tableId,
                CreatedAt = DateTime.UtcNow,
                Status = enSessionStatus.PendingActivation,
                ClosedAt = null,
            };

            var deviceSession = new DeviceSession
            {
                DeviceSessionId = deviceSessionId,
                TableSessionId = tableSession.TableSessionId,
                Role = enDeviceRole.Host,
                IsApproved = true,
                TableSession = tableSession,
            };

            await _deviceSessionRepository.AddSessionAsync(deviceSession);
            await _notifier.NotifyCashiersOfActivationAsync(tableId, tableSession.TableSessionId);

            return Result<SessionResponse>.Success(tableSession.ToResponse(deviceSession));
        }

        private async Task<Result<SessionResponse>> JoinTableSessionAsync(TableSession tableSession, Guid deviceSessionId)
        {
            var deviceSession = new DeviceSession
            {
                DeviceSessionId = deviceSessionId,
                TableSessionId = tableSession.TableSessionId,
                Role = enDeviceRole.Guest,
                IsApproved = false,
            };

            await _deviceSessionRepository.AddSessionAsync(deviceSession);
            await _notifier.NotifyHostOfGuestJoinAsync(tableSession.TableSessionId, deviceSessionId);

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(tableSession, deviceSession));
        }

        private Result<SessionResponse> AccessTableSessionAsync(TableSession activeSession, Guid deviceSessionId)
        {
            var deviceSession = activeSession.Devices.FirstOrDefault(d => d.DeviceSessionId == deviceSessionId);

            if (deviceSession == null)
            {
                return Result<SessionResponse>.Failure("Invalid or expired device session", enErrorType.Unauthorized);
            }

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(activeSession, deviceSession));
        }

        private void RevokeSessionInCache(Guid tableSessionId)
        {
            _cache.Set($"revoked_table_session_{tableSessionId}", true, TimeSpan.FromHours(4));
        }

        public async Task<Result<TableSessionResponse>> ActivateTableSessionAsync(ActivateTableSessionRequest request)
        {
            var session = await _tableSessionRepository.GetSessionByIdAsync(request.tableSessionId);

            if (session == null)
            {
                return Result<TableSessionResponse>.Failure("Table session not found.", enErrorType.NotFound);
            }

            if (session.Status != enSessionStatus.PendingActivation)
            {
                return Result<TableSessionResponse>.Failure("Session is not pending activation.", enErrorType.Conflict);
            }

            session.Status = enSessionStatus.Active;
            await _tableSessionRepository.UpdateSessionAsync(session);

            var hostDevice = session.Devices.FirstOrDefault(d => d.Role == enDeviceRole.Host);
            if (hostDevice != null)
            {
                await _notifier.NotifyHostOfTableActivationAsync(hostDevice.DeviceSessionId, session.TableSessionId);
            }

            await _notifier.NotifyCashiersOfTableSessionSyncAsync(session.TableSessionId, session.Status);

            return Result<TableSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request, Guid hostDeviceSessionId)
        {
            var guestDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(request.deviceSessionId);
            if (guestDeviceSession == null)
                return Result<SessionResponse>.Failure("Device session not found.", enErrorType.NotFound);

            var hostDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(hostDeviceSessionId);

            if (hostDeviceSession == null ||
                hostDeviceSession.Role != enDeviceRole.Host ||
                hostDeviceSession.TableSessionId != guestDeviceSession.TableSessionId)
            {
                return Result<SessionResponse>.Failure("Unauthorized to approve guests for this table.", enErrorType.Unauthorized);
            }

            guestDeviceSession.IsApproved = true;
            await _deviceSessionRepository.UpdateDeviceSessionAsync(guestDeviceSession);
            await _notifier.NotifyGuestOfApprovalAsync(guestDeviceSession.DeviceSessionId);

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(guestDeviceSession.TableSession, guestDeviceSession));
        }

        public async Task<Result> DeactivateTableSessionAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result.Failure("No active table session was found.", enErrorType.NotFound);

            if (session.Orders != null && session.Orders.Any(o => o.OrderStatus == enOrderStatus.Preparing || o.OrderStatus == enOrderStatus.Served))
            {
                return Result.Failure("Cannot deactivate session: Active orders are being prepared or served.", enErrorType.Conflict);
            }

            // --- Transaction Wrapper Added ---
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                if (session.Orders != null && session.Orders.Any())
                {
                    await _orderRepository.HardDeleteOrdersAsync(session.Orders);
                }

                await _tableSessionRepository.DeleteSessionAsync(session);

                var table = await _tableRepository.GetTableByIdAsync(session.TableId);
                if (table != null)
                {
                    table.Status = enTableStatus.Available;
                    await _tableRepository.UpdateTableAsync(table);
                }
            });

            RevokeSessionInCache(tableSessionId);

            await _notifier.NotifyCashiersOfTableSessionSyncAsync(session.TableSessionId, enSessionStatus.Closed);

            return Result.Success();
        }

        public async Task<Result> RequestBillAsync(Guid tableSessionId, Guid deviceSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result.Failure("No active table session was found.", enErrorType.NotFound);

            var device = session.Devices.FirstOrDefault(d => d.DeviceSessionId == deviceSessionId);
            if (device == null || device.Role != enDeviceRole.Host)
                return Result.Failure("Only the table host is authorized to request the bill.", enErrorType.Unauthorized);

            if (!session.Orders.Any(o => o.OrderStatus == enOrderStatus.Preparing || o.OrderStatus == enOrderStatus.Served))
                return Result.Failure("No approved orders available for billing.", enErrorType.Validation);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);

            await _notifier.NotifyCashiersOfBillRequestAsync(tableSessionId, table!.TableNumber);
            await _notifier.NotifyGuestsOfBillRequestAsync(tableSessionId);

            table.Status = enTableStatus.Billing;
            await _tableRepository.UpdateTableAsync(table);

            return Result.Success();
        }

        public async Task<Result<SessionResponse>> EndTableSessionAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result<SessionResponse>.Failure("No active table session was found.", enErrorType.NotFound);

            var validOrders = session.Orders.Where(o => o.OrderStatus == enOrderStatus.Preparing || o.OrderStatus == enOrderStatus.Served).ToList();

            if (!validOrders.Any())
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    session.Status = enSessionStatus.Closed;
                    session.ClosedAt = DateTime.UtcNow;

                    var orphanedPendingOrders = session.Orders.Where(o => o.OrderStatus == enOrderStatus.Pending).ToList();
                    foreach (var orphaned in orphanedPendingOrders) orphaned.OrderStatus = enOrderStatus.Cancelled;
                    if (orphanedPendingOrders.Any()) await _orderRepository.UpdateOrdersAsync(orphanedPendingOrders);

                    await _tableSessionRepository.UpdateSessionAsync(session);

                    var table = await _tableRepository.GetTableByIdAsync(session.TableId);
                    if (table != null)
                    {
                        table.Status = enTableStatus.Available;
                        await _tableRepository.UpdateTableAsync(table);
                    }
                });

                RevokeSessionInCache(tableSessionId);
                await _notifier.NotifyCustomerOfSessionEndedAsync(tableSessionId);
                await _notifier.NotifyCashiersOfTableSessionSyncAsync(session.TableSessionId, enSessionStatus.Closed);

                return Result<SessionResponse>.Success(session.ToResponse(null));
            }

            var allOrderItems = validOrders.SelectMany(o => o.OrderItems).ToList();

            var consolidatedItems = allOrderItems
                .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem.NameEn, oi.MenuItem.NameAr, oi.UnitPrice })
                .Select(g => new BillItem
                {
                    MenuItemId = g.Key.MenuItemId,
                    NameEn = g.Key.NameEn,
                    NameAr = g.Key.NameAr,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(oi => oi.Quantity),
                    TotalPrice = g.Key.UnitPrice * g.Sum(oi => oi.Quantity)
                }).ToList();

            decimal totalSubTotal = consolidatedItems.Sum(i => i.TotalPrice);
            int totalItemsCount = consolidatedItems.Sum(i => i.Quantity);

            int uniqueGuestsCount = session.Devices.Count(d => d.IsApproved);

            var activeTaxes = await _taxRepository.GetActiveTaxesAsync();
            var taxResult = _taxCalculationService.CalculateTaxes(totalSubTotal, uniqueGuestsCount, totalItemsCount, activeTaxes);

            var finalBill = new Bill
            {
                TableSessionId = session.TableSessionId,
                TotalSubTotal = totalSubTotal,
                TotalTax = taxResult.TotalTaxAmount,
                GrandTotal = totalSubTotal + taxResult.TotalTaxAmount,
                CreatedAt = DateTime.UtcNow,
                BillItems = consolidatedItems,
                BillTaxes = taxResult.AppliedTaxes.Select(t => new BillTax
                {
                    TaxNameEn = t.NameEn,
                    TaxNameAr = t.NameAr,
                    AppliedAmount = t.Amount
                }).ToList()
            };

            // --- Transaction Wrapper Maintained ---
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _billRepository.AddBillAsync(finalBill);

                session.Status = enSessionStatus.Closed;
                session.ClosedAt = DateTime.UtcNow;

                foreach (var order in validOrders) order.OrderStatus = enOrderStatus.Served;
                await _orderRepository.UpdateOrdersAsync(validOrders);

                var orphanedPendingOrders = session.Orders.Where(o => o.OrderStatus == enOrderStatus.Pending).ToList();
                foreach (var orphaned in orphanedPendingOrders) orphaned.OrderStatus = enOrderStatus.Cancelled;
                if (orphanedPendingOrders.Any()) await _orderRepository.UpdateOrdersAsync(orphanedPendingOrders);

                await _tableSessionRepository.UpdateSessionAsync(session);

                var table = await _tableRepository.GetTableByIdAsync(session.TableId);
                if (table != null)
                {
                    table.Status = enTableStatus.Available;
                    await _tableRepository.UpdateTableAsync(table);
                }
            });

            RevokeSessionInCache(tableSessionId);
            await _notifier.NotifyCustomerOfSessionEndedAsync(tableSessionId);
            await _notifier.NotifyCashiersOfTableSessionSyncAsync(session.TableSessionId, enSessionStatus.Closed);

            return Result<SessionResponse>.Success(session.ToResponse(null));
        }

        public async Task<Result> DismissTableSessionAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetSessionByIdAsync(tableSessionId);
            if (session == null)
                return Result.Failure("Table session not found.", enErrorType.NotFound);

            if (session.Status != enSessionStatus.PendingActivation)
                return Result.Failure("Session is not pending activation.", enErrorType.Conflict);

            // --- Transaction Wrapper Added ---
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var table = await _tableRepository.GetTableByIdAsync(session.TableId);
                if (table != null)
                {
                    table.Status = enTableStatus.Available;
                    await _tableRepository.UpdateTableAsync(table);
                }

                session.Status = enSessionStatus.Closed;
                session.ClosedAt = DateTime.UtcNow;
                await _tableSessionRepository.UpdateSessionAsync(session);
            });

            await _notifier.NotifyCustomerOfActivationDismissedAsync(tableSessionId);
            RevokeSessionInCache(tableSessionId);

            return Result.Success();
        }

        public async Task<Result> DismissBillAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result.Failure("No active table session was found.", enErrorType.NotFound);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table == null)
                return Result.Failure("Table not found.", enErrorType.NotFound);

            if (table.Status == enTableStatus.Billing)
            {
                table.Status = enTableStatus.Occupied;
                await _tableRepository.UpdateTableAsync(table);
            }

            await _notifier.NotifyCustomerOfBillRejectedAsync(tableSessionId);
            await _notifier.NotifyCashiersOfBillSyncAsync(tableSessionId);

            return Result.Success();
        }

        public async Task<Result> CleanupZombieSessionsAsync(TimeSpan expirationWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(expirationWindow);
            var expiredSessions = await _tableSessionRepository.GetExpiredPendingSessionsAsync(cutoffTime);

            if (!expiredSessions.Any())
                return Result.Success();

            foreach (var session in expiredSessions)
            {
                // --- Transaction Wrapper Added (Per-Session) ---
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    session.Status = enSessionStatus.Closed;
                    session.ClosedAt = DateTime.UtcNow;

                    if (session.Table != null)
                    {
                        session.Table.Status = enTableStatus.Available;
                        await _tableRepository.UpdateTableAsync(session.Table);
                    }

                    await _tableSessionRepository.UpdateSessionAsync(session);
                });

                await _notifier.NotifyCustomerOfActivationDismissedAsync(session.TableSessionId);
                await _notifier.NotifyCashiersOfZombieSessionClearedAsync(session.TableSessionId);

                RevokeSessionInCache(session.TableSessionId);
            }

            return Result.Success();
        }

        public async Task<Result> RejectJoiningRequestAsync(Guid guestDeviceSessionId, Guid hostDeviceSessionId)
        {
            var guestDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(guestDeviceSessionId);
            if (guestDeviceSession == null)
                return Result.Failure("Device session not found.", enErrorType.NotFound);

            var hostDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(hostDeviceSessionId);

            if (hostDeviceSession == null ||
                hostDeviceSession.Role != enDeviceRole.Host ||
                hostDeviceSession.TableSessionId != guestDeviceSession.TableSessionId)
            {
                return Result.Failure("Unauthorized to reject guests for this table.", enErrorType.Unauthorized);
            }

            // Terminate the session and notify the guest
            await _deviceSessionRepository.DeleteDeviceSessionAsync(guestDeviceSession);
            await _notifier.NotifyGuestOfRejectionAsync(guestDeviceSession.DeviceSessionId);

            return Result.Success();
        }
    }
}