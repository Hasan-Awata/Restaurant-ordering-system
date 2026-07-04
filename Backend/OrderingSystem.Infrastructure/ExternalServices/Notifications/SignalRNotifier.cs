using Microsoft.AspNetCore.SignalR;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Infrastructure.ExternalServices.Notifications;

namespace OrderingSystem.Infrastructure.Notifications
{
    public class SignalRNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<TableSessionNotificationsHub, ITableSessionNotificationsHub> _hubContext;

        public SignalRNotifier(IHubContext<TableSessionNotificationsHub, ITableSessionNotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyCashiersOfActivationAsync(int tableId, Guid tableSessionId)
        {
            await _hubContext.Clients.Group(TableSessionNotificationsHub.GroupNames.Cashiers)
                .ReceiveActivationRequest(tableId, tableSessionId, $"Table {tableId} is requesting activation.");
        }

        public async Task NotifyHostOfGuestJoinAsync(Guid tableSessionId, Guid guestDeviceSessionId)
        {
            // The Host's frontend will have called JoinTableSessionGroup with this ID
            await _hubContext.Clients.Group(tableSessionId.ToString())
                .ReceiveJoinRequest(guestDeviceSessionId, "A new guest is requesting to join your table.");
        }

        public async Task NotifyGuestOfApprovalAsync(Guid guestDeviceSessionId)
        {
            // The Guest's frontend will have called RegisterDeviceGroup with this ID
            await _hubContext.Clients.Group(guestDeviceSessionId.ToString())
                .ReceiveHostApprovalNotification("Your request to join the table has been approved.");
        }

        public async Task NotifyHostOfTableActivationAsync(Guid hostDeviceSessionId)
        {
            // The Host connects to a group named after their DeviceSessionId while waiting
            await _hubContext.Clients.Group(hostDeviceSessionId.ToString())
                .ReceiveCashierApprovalNotification(hostDeviceSessionId, "Your table has been approved by the cashier and is now active.");
        }
    }
}