using Microsoft.Extensions.Caching.Memory;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Bills;
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
            _notifier = notifier;
            _cache = cache;
        }

        public async Task<Result<SessionResponse>> ProcessTableQrCodeAsync(string qrCode, Guid? deviceSessionId = null)
        {
            var table = await _tableSessionRepository.GetTableWithActiveSessionAsync(qrCode);

            if (table == null)
            {
                return Result<SessionResponse>.Failure("Table was not found.", enErrorType.NotFound);
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

            table.Status = enTableStatus.Occupied;

            await _tableRepository.UpdateTableAsync(table);

            return await ActivateTableSessionAsync(table.TableId, Guid.CreateVersion7());
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

            // Optimization: EF Core Graph Insertion. 
            // Saving the DeviceSession automatically saves the attached TableSession! (1 Round Trip)
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

        // Changed from 'async Task' to synchronous 'Result' because we no longer query the DB here!
        private Result<SessionResponse> AccessTableSessionAsync(TableSession activeSession, Guid deviceSessionId)
        {
            // Search for the device in the list we already loaded into memory
            var deviceSession = activeSession.Devices.FirstOrDefault(d => d.DeviceSessionId == deviceSessionId);

            if (deviceSession == null)
            {
                return Result<SessionResponse>.Failure("Invalid or expired device session", enErrorType.Unauthorized);
            }

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(activeSession, deviceSession));
        }

        private void RevokeSessionInCache(Guid tableSessionId)
        {
            // Retain revocation flag for 4 hours to match JWT lifespan
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

            // Process Activation
            session.Status = enSessionStatus.Active;
            await _tableSessionRepository.UpdateSessionAsync(session);

            // Alert the Host that the menu is now unlocked
            var hostDevice = session.Devices.FirstOrDefault(d => d.Role == enDeviceRole.Host);
            if (hostDevice != null)
            {
                await _notifier.NotifyHostOfTableActivationAsync(session.TableSessionId);
            }

            return Result<TableSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request, Guid hostDeviceSessionId)
        {
            var guestDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(request.deviceSessionId);
            if (guestDeviceSession == null)
                return Result<SessionResponse>.Failure("Device session not found.", enErrorType.NotFound);

            // Fetch the host's session to verify authority
            var hostDeviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(hostDeviceSessionId);

            // Validate role and table session match
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

            if (session.Orders != null && session.Orders.Any())
            {
                foreach (var order in session.Orders.ToList())
                {
                    await _orderRepository.DeleteOrderAsync(order);
                }
            }

            await _tableSessionRepository.DeleteSessionAsync(session);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = enTableStatus.Available;
                await _tableRepository.UpdateTableAsync(table);
            }

            // Revoke stateless tokens for this session
            RevokeSessionInCache(tableSessionId);

            return Result.Success();
        }

        public async Task<Result> RequestBillAsync(Guid tableSessionId, Guid deviceSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result.Failure("No active table session was found.", enErrorType.NotFound);

            // Security check: Verify the device belongs to this table session
            if (!session.Devices.Any(d => d.DeviceSessionId == deviceSessionId))
                return Result.Failure("You are not authorized to request the bill for this table.", enErrorType.Unauthorized);

            if (session.Orders.Count == 0)
                return Result.Failure("No orders available for billing.", enErrorType.Validation);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);

            // This acts as the single "AcceptPayment" notification to the cashier
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

            // 1. Filter out cancelled orders and flatten the items
            var validOrders = session.Orders.Where(o => o.OrderStatus != enOrderStatus.Cancelled).ToList();
            var allOrderItems = validOrders.SelectMany(o => o.OrderItems).ToList();

            // 2. Consolidate matching items for the final receipt
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

            // 3. Perform Tax Calculations using your existing service
            decimal totalSubTotal = consolidatedItems.Sum(i => i.TotalPrice);
            int totalItemsCount = consolidatedItems.Sum(i => i.Quantity);
            int uniqueGuestsCount = session.Devices.Count;

            var activeTaxes = await _taxRepository.GetActiveTaxesAsync();
            var taxResult = _taxCalculationService.CalculateTaxes(totalSubTotal, uniqueGuestsCount, totalItemsCount, activeTaxes);

            // 4. Create the Immutable Snapshot (The Bill)
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

            // Save the bill snapshot to the database
            await _billRepository.AddBillAsync(finalBill);

            // 5. Proceed with closing the session (Your existing logic)
            session.Status = enSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            foreach (var order in validOrders)
            {
                order.OrderStatus = enOrderStatus.Served;
                await _orderRepository.UpdateOrderAsync(order);
            }

            await _tableSessionRepository.UpdateSessionAsync(session);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = enTableStatus.Available;
                await _tableRepository.UpdateTableAsync(table);
            }

            RevokeSessionInCache(tableSessionId);
            await _notifier.NotifyCustomerOfSessionEndedAsync(tableSessionId);

            return Result<SessionResponse>.Success(session.ToResponse(null));
        }

        public async Task<Result> DismissTableSessionAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetSessionByIdAsync(tableSessionId);
            if (session == null)
                return Result.Failure("Table session not found.", enErrorType.NotFound);

            if (session.Status != enSessionStatus.PendingActivation)
                return Result.Failure("Session is not pending activation.", enErrorType.Conflict);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = enTableStatus.Available;
                await _tableRepository.UpdateTableAsync(table);
            }

            await _notifier.NotifyCustomerOfActivationDismissedAsync(tableSessionId);

            session.Status = enSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await _tableSessionRepository.UpdateSessionAsync(session);

            // Revoke stateless tokens for this session
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

            return Result.Success();
        }
    }
}