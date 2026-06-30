using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Domain.Common;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;

namespace OrderingSystem.Application.Services
{
    public class SessionCommandService : ISessionCommandService
    {
        private readonly ITableSessionRepository _tableSessionRepository;
        private readonly IDeviceSessionRepository _deviceSessionRepository;
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

        public async Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request)
        {
            var deviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(request.deviceSessionId);

            if (deviceSession == null)
            {
                return Result<SessionResponse>.Failure("Device session not found.", enErrorType.NotFound);
            }

            deviceSession.IsApproved = true;
            await _deviceSessionRepository.UpdateDeviceSessionAsync(deviceSession);

            await _notifier.NotifyGuestOfApprovalAsync(deviceSession.DeviceSessionId);

            return Result<SessionResponse>.Success(SessionsMappers.ToResponse(deviceSession.TableSession, deviceSession));
        }

        public async Task<Result<SessionResponse>> DeactivateAsync(DeactivateSessionByAdminRequest request)
        {
            return Result<SessionResponse>.Failure("Not Implemented", enErrorType.Failure);
        }
    }
}