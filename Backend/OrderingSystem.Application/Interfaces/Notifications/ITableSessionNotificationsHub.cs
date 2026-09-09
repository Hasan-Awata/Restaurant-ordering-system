using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface ITableSessionNotificationsHub
    {
        public Task ReceiveActivationRequest(int tableId, Guid tableSessionId, string message);
        public Task ReceiveJoinRequest(Guid deviceSessionId, string message);
        public Task ReceiveHostApprovalNotification(string message);
        public Task ReceiveCashierApprovalNotification(Guid tableSessionId, string message);
        public Task ReceiveNewOrderNotification(int orderId, Guid tableSessionId, string message);
        public Task ReceiveOrderStatusUpdate(int orderId, enOrderStatus status, string message);
        public Task ReceiveBillRequestNotification(Guid tableSessionId, int tableNumber, string message);
        public Task ReceiveMenuUpdated(string message);
        public Task ReceiveActivationDismissed(string message);
        public Task ReceiveBillRejected(string message);
        public Task ReceiveOrderRejected(int orderId, string message);
        public Task ReceiveSessionEnded(string message);
        public Task ReceiveBillRequested(string tableSessionId);
    }
}
