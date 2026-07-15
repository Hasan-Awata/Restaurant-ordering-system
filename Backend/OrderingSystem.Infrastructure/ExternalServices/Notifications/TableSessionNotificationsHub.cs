using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
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
            var httpContext = Context.GetHttpContext();
            bool isAuthenticated = false;

            // 1. Check JWT (Staff Authentication)
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                isAuthenticated = true;
                if (role == enRoleType.Cashier.ToString() || role == enRoleType.Admin.ToString())
                    await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Cashiers);
                else if (role == enRoleType.Waiter.ToString())
                    await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Waiters);
            }

            // 2. Check Query String OR Cookie (Customer Authentication)
            if (!isAuthenticated && httpContext != null)
            {

                httpContext.Request.Cookies.TryGetValue("DeviceSessionId", out string? deviceIdStr);

                //// First try to grab the DeviceSessionId from the Query String 
                //string? deviceIdStr = httpContext.Request.Query["DeviceSessionId"];

                //// Fallback to Cookie if it's missing from the Query String
                //if (string.IsNullOrEmpty(deviceIdStr))
                //{
                //    httpContext.Request.Cookies.TryGetValue("DeviceSessionId", out deviceIdStr);
                //}

                if (!string.IsNullOrEmpty(deviceIdStr) && Guid.TryParse(deviceIdStr, out var deviceSessionId))
                {
                    // SECURE LOOKUP: Use the token to find the table session in the DB
                    var dbContext = httpContext.RequestServices.GetRequiredService<OrderingSystemDbContext>();

                    var deviceRecord = await dbContext.SessionDevices
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.DeviceSessionId == deviceSessionId);

                    if (deviceRecord != null)
                    {
                        isAuthenticated = true;

                        // Add to private device group (for personal order updates)
                        await Groups.AddToGroupAsync(Context.ConnectionId, deviceSessionId.ToString());

                        // Add to table group (for table-wide events like bill approvals)
                        await Groups.AddToGroupAsync(Context.ConnectionId, deviceRecord.TableSessionId.ToString());
                    }
                }
            }

            // 3. Reject unauthenticated or invalid connections completely
            if (!isAuthenticated)
            {
                Context.Abort(); // This is what triggers the {"type":7} message!
                return;
            }

            await base.OnConnectedAsync();
        }
    }
}