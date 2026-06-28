using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Domain.Common;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
//using Microsoft.AspNetCore.SignalR; 
//using OrderingSystem.Application.Hubs;

namespace OrderingSystem.Application.Services
{
    public class SessionCommandService : ISessionCommandService
    {
        private readonly ITableSessionRepository _tableSessionRepository;
        private readonly ITableSessionQuery _tableSessionQuery;
        private readonly IDeviceSessionRepository _deviceSessionRepository;
        private readonly IDeviceSessionQuery _deviceSessionQuery;
        private readonly IRealTimeNotifier _notifier;
        public SessionCommandService(
            ITableSessionRepository tableSessionRepository,
            IDeviceSessionRepository deviceSessionRepository,
            IDeviceSessionQuery deviceSessionQuery,
            ITableSessionQuery tableSessionQuery,
            IRealTimeNotifier notifier)
        {
            _tableSessionRepository = tableSessionRepository;
            _deviceSessionRepository = deviceSessionRepository;
            _deviceSessionQuery = deviceSessionQuery;
            _tableSessionQuery = tableSessionQuery;
            _notifier = notifier;
        }

        public async Task<Result<SessionResponse>> ProcessTableQrCodeAsync(ProcessQrCodeRequest request)
        {
            var table = await _tableSessionQuery.GetTableWithActiveSessionAsync(request.qrCode);

            if (table == null)
            {
                return Result<SessionResponse>.Failure("Table was not found.", enErrorType.NotFound);
            }

            // A live table session is running
            if (table.Session != null)
            {
                // Corrected Logic: If they have an ID, they are reconnecting/accessing
                if (request.deviceSessionId.HasValue)
                {
                    return await AccessTableSessionAsync(table.Session.TableSessionId, request.deviceSessionId.Value);
                }

                // If they don't have an ID, they are a new guest joining the existing session
                Guid newGuestDeviceId = Guid.CreateVersion7();
                return await JoinTableSessionAsync(table.Session, newGuestDeviceId);
            }

            // No active session -> Brand new activation request by a Host
            Guid newHostDeviceId = Guid.CreateVersion7();
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
                IsApproved = true, // The Host is auto-approved by default, waiting on cashier
                TableSession = tableSession,
            };

            await _tableSessionRepository.AddSessionAsync(tableSession);
            await _deviceSessionRepository.AddSessionAsync(deviceSession);

            // Real-Time Notification: Send alert to all connected Cashiers/Admins
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

            // Real-Time Notification: Alert the specific Table Session Room (The Host)
            await _notifier.NotifyHostOfGuestJoinAsync(tableSession.TableSessionId, deviceSessionId);

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(tableSession, deviceSession));
        }

        private async Task<Result<SessionResponse>> AccessTableSessionAsync(Guid tableSessionId, Guid deviceSessionId)
        {
            // Implementation Note: Fetch the device from DB. 
            // If it exists and matches tableSessionId, map to success response to restore UI state.
            // If it doesn't match, return an unauthorized/hack attempt failure.
            return Result<SessionResponse>.Failure("Not Implemented", enErrorType.Failure);
        }

        public async Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request)
        {
            var deviceSession = await _deviceSessionQuery.GetDeviceSessionByIdAsync(request.deviceSessionId);

            if (deviceSession == null)
            {
                return Result<SessionResponse>.Failure("Device session not found.", enErrorType.NotFound);
            }

            deviceSession.IsApproved = true;
            await _deviceSessionRepository.UpdateDeviceSessionAsync(deviceSession);

            // Real-Time Notification: Notify the specific guest that they have been approved
            await _notifier.NotifyGuestOfApprovalAsync(deviceSession.DeviceSessionId);

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(deviceSession.TableSession, deviceSession));
        }

        public async Task<Result<SessionResponse>> DeactivateAsync(DeactivateSessionByAdminRequest request)
        {
            return Result<SessionResponse>.Failure("Not Implemented", enErrorType.Failure);
        }
    }
}