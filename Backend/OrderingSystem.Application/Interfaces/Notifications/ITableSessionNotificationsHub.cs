using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface ITableSessionNotificationsHub
    {
        public Task ReceiveActivationRequest(int tableId, Guid tableSessionId, string message);
        public Task ReceiveJoinRequest(Guid deviceSessionId, string message);
        public Task ReceiveApprovalNotification(string message);

        // Add future notifications here (e.g., ReceiveNewOrder, ReceivePaymentAlert)
    }
}
