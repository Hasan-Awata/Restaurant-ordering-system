using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
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
        private readonly IRealTimeNotifier _notifier;

        public SessionCommandService(
            ITableSessionRepository tableSessionRepository,
            IDeviceSessionRepository deviceSessionRepository,
            ITableRepository tableRepository,
            IOrderRepository orderRepository,
            IRealTimeNotifier notifier)
        {
            _tableSessionRepository = tableSessionRepository;
            _deviceSessionRepository = deviceSessionRepository;
            _tableRepository = tableRepository;
            _orderRepository = orderRepository;
            _notifier = notifier;
        }

        public async Task<Result<SessionResponse>> ProcessTableQrCodeAsync(string qrCode, Guid? deviceSessionId = null)
        {
            // PRO-TIP: Ensure this query in TableSessionQuery.cs now uses:
            // .Include(t => t.Sessions.Where(s => s.Status != enSessionStatus.Closed))
            // .ThenInclude(s => s.Devices) <--- Add this so we have devices in memory!
            var table = await _tableSessionRepository.GetTableWithActiveSessionAsync(qrCode);

            if (table == null)
            {
                return Result<SessionResponse>.Failure("Table was not found.", enErrorType.NotFound);
            }

            var activeSession = table.Sessions.FirstOrDefault();

            if (activeSession != null)
            {
                if (activeSession.Status == enSessionStatus.PendingActivation)
                {
                    // Let the recognized Host reconnect to retrieve their session state
                    if (deviceSessionId.HasValue && activeSession.Devices.Any(d => d.DeviceSessionId == deviceSessionId.Value))
                    {
                        return AccessTableSessionAsync(activeSession, deviceSessionId.Value);
                    }

                    return Result<SessionResponse>.Failure("Table session is pending activation.", enErrorType.Conflict);
                }

                if (deviceSessionId.HasValue)
                {
                    return AccessTableSessionAsync(activeSession, deviceSessionId.Value);
                }

                Guid newGuestDeviceId = Guid.CreateVersion7();
                return await JoinTableSessionAsync(activeSession, newGuestDeviceId);
            }

            Guid newHostDeviceId = Guid.CreateVersion7();
            table.Status = enTableStatus.Occupied;
            return await ActivateTableSessionAsync(table.TableId, newHostDeviceId);
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

            // 1. Explicitly delete orders to safely bypass the ON DELETE RESTRICT database constraint
            if (session.Orders != null && session.Orders.Any())
            {
                foreach (var order in session.Orders.ToList())
                {
                    await _orderRepository.DeleteOrderAsync(order);
                }
            }

            // 2. Hard delete the session (DeviceSessions will cascade automatically!)
            await _tableSessionRepository.DeleteSessionAsync(session);

            // 3. Reset the Table Status back to Available
            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = enTableStatus.Available;
                await _tableRepository.UpdateTableAsync(table);
            }

            // Optional: Notify via SignalR that session is dropped
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

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);

            // Notify the cashiers
            await _notifier.NotifyCashiersOfBillRequestAsync(tableSessionId, table!.TableNumber);

            table.Status = enTableStatus.Billing;
            await _tableRepository.UpdateTableAsync(table);

            return Result.Success();
        }

        public async Task<Result> ApproveBillAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result.Failure("No active table session was found.", enErrorType.NotFound);

            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table == null)
                return Result.Failure("Table not found.", enErrorType.NotFound);

            // Change table status to Billing
            table.Status = enTableStatus.Billing;
            await _tableRepository.UpdateTableAsync(table);

            // Notify the customer
            await _notifier.NotifyCustomerOfBillApprovalAsync(tableSessionId);

            return Result.Success();
        }

        public async Task<Result<SessionResponse>> EndTableSessionAsync(Guid tableSessionId)
        {
            var session = await _tableSessionRepository.GetActiveTableSessionWithOrdersAndDevicesAsync(tableSessionId);
            if (session == null)
                return Result<SessionResponse>.Failure("No active table session was found.", enErrorType.NotFound);

            // 1. Soft close the session
            session.Status = enSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            // 2. Flag all active orders as Served (excluding already cancelled ones)
            if (session.Orders != null && session.Orders.Any())
            {
                foreach (var order in session.Orders.Where(o => o.OrderStatus != enOrderStatus.Cancelled))
                {
                    order.OrderStatus = enOrderStatus.Served;
                    await _orderRepository.UpdateOrderAsync(order);
                }
            }

            // 3. Save the session status
            await _tableSessionRepository.UpdateSessionAsync(session);

            // 4. Reset the Table Status back to Available for the next customer
            var table = await _tableRepository.GetTableByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = enTableStatus.Available;
                await _tableRepository.UpdateTableAsync(table);
            }

            // Optional: Notify clients via SignalR to show the "Thank You" or "Receipt" screen

            return Result<SessionResponse>.Success(session.ToResponse(null));
        }
    }
}