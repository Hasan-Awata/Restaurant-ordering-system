using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Domain.Enums;
using System.Security.Claims;

namespace OrderingSystem.Infrastructure.ExternalServices.Notifications
{
    [Authorize]
    public class TableSessionNotificationsHub : Hub<ITableSessionNotificationsHub>
    {
        public static class GroupNames
        {
            public const string Cashiers = "Group_Cashiers";
            public const string Waiters = "Group_Waiters";
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                Context.Abort();
                return;
            }

            // 1. Staff Group Routing
            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                if (role == enRoleType.Cashier.ToString() || role == enRoleType.Admin.ToString())
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Cashiers);
                }
                else if (role == enRoleType.Waiter.ToString())
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Waiters);
                }

                await base.OnConnectedAsync();
                return;
            }

            // 2. Customer Group Routing (Derived straight from claims)
            var deviceSessionId = user.FindFirst("DeviceSessionId")?.Value;
            var tableSessionId = user.FindFirst("TableSessionId")?.Value;

            if (!string.IsNullOrEmpty(deviceSessionId) && !string.IsNullOrEmpty(tableSessionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, deviceSessionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, tableSessionId);

                await base.OnConnectedAsync();
                return;
            }

            // Abort if identity lacks appropriate claims
            Context.Abort();
        }
    }
}