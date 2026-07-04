using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface ITableSessionNotificationsHub
    {
        public Task ReceiveActivationRequest(int tableId, Guid tableSessionId, string message);
        public Task ReceiveJoinRequest(Guid deviceSessionId, string message);
        public Task ReceiveHostApprovalNotification(string message);
        public Task ReceiveCashierApprovalNotification(Guid hostDeviceSessionId, string message);
    }
}
