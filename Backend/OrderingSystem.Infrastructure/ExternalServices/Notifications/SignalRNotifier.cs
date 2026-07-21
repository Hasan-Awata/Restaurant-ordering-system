using Microsoft.AspNetCore.SignalR;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Infrastructure.ExternalServices.Notifications;
using OrderingSystem.Domain.Enums;

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

        public async Task NotifyHostOfTableActivationAsync(Guid tableSessionId)
        {
            // The Host connects to a group named after their DeviceSessionId while waiting
            await _hubContext.Clients.Group(tableSessionId.ToString())
                .ReceiveCashierApprovalNotification(tableSessionId, "Your table has been approved by the cashier and is now active.");
        }

        public async Task NotifyCashierOfNewOrderAsync(int orderId, Guid tableSessionId)
        {
            // Target the cashiers group 
            await _hubContext.Clients.Group(TableSessionNotificationsHub.GroupNames.Cashiers)
                .ReceiveNewOrderNotification(orderId, tableSessionId, $"New order #{orderId} received.");
        }

        public async Task NotifyCustomerOfOrderStatusAsync(Guid deviceSessionId, int orderId, enOrderStatus status)
        {
            // Target the specific device session 
            await _hubContext.Clients.Group(deviceSessionId.ToString())
                .ReceiveOrderStatusUpdate(orderId, status, $"Your order #{orderId} is now {status}.");
        }

        public async Task NotifyCashiersOfCustomerCancellationAsync(int orderId, Guid tableSessionId)
        {
            // Alert the cashiers that an order was dropped
            await _hubContext.Clients.Group(TableSessionNotificationsHub.GroupNames.Cashiers)
                .ReceiveOrderStatusUpdate(orderId, enOrderStatus.Cancelled, $"Order #{orderId} was cancelled by the customer.");
        }

        public async Task NotifyCashiersOfBillRequestAsync(Guid tableSessionId, int tableNumber)
        {
            await _hubContext.Clients.Group(TableSessionNotificationsHub.GroupNames.Cashiers)
                .ReceiveBillRequestNotification(tableSessionId, tableNumber, $"Table {tableNumber} is requesting the bill.");
        }

        public async Task NotifyCustomerOfBillApprovalAsync(Guid tableSessionId)
        {
            // Broadcast to the entire table session group so all devices at the table know the bill is ready
            await _hubContext.Clients.Group(tableSessionId.ToString())
                .ReceiveBillApprovalNotification(tableSessionId, "Your bill has been prepared and approved by the cashier.");
        }
    }
}