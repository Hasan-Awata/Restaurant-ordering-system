using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface IRealTimeNotifier
    {
        public Task NotifyCashiersOfActivationAsync(int tableId, Guid tableSessionId);
        public Task NotifyHostOfGuestJoinAsync(Guid tableSessionId, Guid guestDeviceSessionId);
        public Task NotifyGuestOfApprovalAsync(Guid guestDeviceSessionId);
        public Task NotifyHostOfTableActivationAsync(Guid hostDeviceSessionId);
    }
}
