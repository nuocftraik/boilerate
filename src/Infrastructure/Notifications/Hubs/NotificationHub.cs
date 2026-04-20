using Boilerate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Boilerate.Infrastructure.Notifications.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ICurrentUser _currentUser;

    public NotificationHub(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Called when client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Get current user ID
        var userId = _currentUser.GetUserId();

        // Add connection to user group (for targeting specific users)
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        }

        // Add connection to role groups (for role-based notifications)
        var roles = _currentUser.GetRoles();
        if (roles != null)
        {
            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Cleanup is automatic when connection closes
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client method to mark notification as read
    /// </summary>
    public async Task MarkAsRead(Guid notificationId)
    {
        // Can trigger server-side logic here if needed
        await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
    }
}
