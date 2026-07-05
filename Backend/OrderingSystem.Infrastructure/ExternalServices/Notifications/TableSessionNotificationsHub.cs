using Microsoft.AspNetCore.SignalR;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Domain.Enums;
using System.Security.Claims;

namespace OrderingSystem.Infrastructure.ExternalServices.Notifications
{
    public class TableSessionNotificationsHub : Hub<ITableSessionNotificationsHub>
    {
        // 1. RENAME THIS to avoid colliding with the base Hub.Groups property
        public static class GroupNames
        {
            public const string Cashiers = "Group_Cashiers";
            public const string Waiters = "Group_Waiters";
        }

        public override async Task OnConnectedAsync()
        {
            // Example: Auto-assign authenticated staff to groups based on their JWT claims
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (role == enRoleType.Cashier.ToString() || role == enRoleType.Admin.ToString())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Cashiers);
            }
            else if (role == enRoleType.Waiter.ToString())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Waiters);
            }

            await base.OnConnectedAsync();
        }

        // Table hosts/guests explicitly join a room for their specific session
        public async Task JoinTableSessionGroup(Guid tableSessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tableSessionId.ToString());
        }

        public async Task LeaveTableSessionGroup(Guid tableSessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tableSessionId.ToString());
        }

        // Similarly, target specific devices by their unique ID
        public async Task RegisterDeviceGroup(Guid deviceSessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, deviceSessionId.ToString());
        }
    }
}