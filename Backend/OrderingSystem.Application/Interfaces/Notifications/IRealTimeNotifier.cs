using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Interfaces.Notifications
{
    public interface IRealTimeNotifier
    {
        public Task NotifyCashiersOfActivationAsync(int tableId, Guid tableSessionId, int floorNumber);
        public Task NotifyHostOfGuestJoinAsync(Guid tableSessionId, Guid guestDeviceSessionId);
        public Task NotifyGuestOfApprovalAsync(Guid guestDeviceSessionId);
        public Task NotifyHostOfTableActivationAsync(Guid hostDeviceSessionId, Guid tableSessionId);
        public Task NotifyCashierOfNewOrderAsync(int orderId, Guid tableSessionId, int floorNumber);
        public Task NotifyCustomerOfOrderStatusAsync(Guid deviceSessionId, int orderId, enOrderStatus status);
        public Task NotifyCashiersOfCustomerCancellationAsync(int orderId, Guid tableSessionId, int floorNumber);
        public Task NotifyCashiersOfBillRequestAsync(Guid tableSessionId, int tableNumber, int floorNumber);
        public Task NotifyMenuUpdatedAsync();
        public Task NotifyCustomerOfActivationDismissedAsync(Guid tableSessionId);
        public Task NotifyCustomerOfBillRejectedAsync(Guid tableSessionId);
        public Task NotifyCustomerOfOrderRejectedAsync(Guid deviceSessionId, int orderId);
        public Task NotifyCustomerOfSessionEndedAsync(Guid tableSessionId);
        public Task NotifyGuestsOfBillRequestAsync(Guid tableSessionId);
        public Task NotifyTaxesUpdatedAsync();
        public Task NotifyGuestOfRejectionAsync(Guid guestDeviceSessionId);
        public Task NotifyCashiersOfZombieSessionClearedAsync(Guid tableSessionId, int floorNumber);
        public Task NotifyCashiersOfOrderSyncAsync(int orderId, enOrderStatus status, int floorNumber);
        public Task NotifyCashiersOfTableSessionSyncAsync(Guid tableSessionId, enSessionStatus status, int floorNumber);
        public Task NotifyCashiersOfBillSyncAsync(Guid tableSessionId, int floorNumber);
    }
}