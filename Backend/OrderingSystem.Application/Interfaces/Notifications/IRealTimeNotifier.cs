using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface IRealTimeNotifier
    {
        public Task NotifyCashiersOfActivationAsync(int tableId, Guid tableSessionId);
        public Task NotifyHostOfGuestJoinAsync(Guid tableSessionId, Guid guestDeviceSessionId);
        public Task NotifyGuestOfApprovalAsync(Guid guestDeviceSessionId);
        public Task NotifyHostOfTableActivationAsync(Guid tableSessionId);
        public Task NotifyCashierOfNewOrderAsync(int orderId, Guid tableSessionId);
        public Task NotifyCustomerOfOrderStatusAsync(Guid deviceSessionId, int orderId, enOrderStatus status);
        public Task NotifyCashiersOfCustomerCancellationAsync(int orderId, Guid tableSessionId);
        public Task NotifyCashiersOfBillRequestAsync(Guid tableSessionId, int tableNumber);
        public Task NotifyCustomerOfBillApprovalAsync(Guid tableSessionId);
    }
}
