
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace StoreManagement.Api.Hubs;

[Authorize]
public class DeviceNotificationHub : Hub
{
    public const string SystemAdminsGroup = "SystemAdmins";

    public static string UserGroup(string userId)
    {
        return $"User_{userId}";
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            // System administrators receive administrative
            // notification events.
            if (user.IsInRole("SystemAdmin"))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    SystemAdminsGroup);
            }

            // Resolve the user ID from common JWT claim names.
            var userId =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("userId")?.Value
                ?? user.FindFirst("user_id")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    UserGroup(userId));
            }
        }

        await base.OnConnectedAsync();
    }
}
