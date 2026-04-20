using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Application.Common.Models;
using Boilerate.Application.Common.Persistence;
using Boilerate.Application.Notifications;
using Boilerate.Domain.Notifications;
using Boilerate.Infrastructure.Notifications.Hubs;
using Mapster;
using Microsoft.AspNetCore.SignalR;

namespace Boilerate.Infrastructure.Notifications;

/// <summary>
/// Notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _repository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ICurrentUser _currentUser;

    public NotificationService(
        IRepository<Notification> repository,
        IHubContext<NotificationHub> hubContext,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _hubContext = hubContext;
        _currentUser = currentUser;
    }

    public async Task<Guid> SendToUserAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string? referenceType = null,
        Guid? referenceId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        // Create notification entity
        var notification = Notification.CreateForUser(
            userId, title, message, type, referenceType, referenceId, actionUrl);

        // Save to database
        await _repository.AddAsync(notification, cancellationToken);

        // Send via SignalR
        var pushDto = notification.Adapt<NotificationPushDto>();
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", pushDto, cancellationToken);

        // Mark as sent
        notification.MarkAsSent();
        await _repository.UpdateAsync(notification, cancellationToken);

        return notification.Id;
    }

    public async Task<Guid> SendToRoleAsync(
        string role,
        string title,
        string message,
        NotificationType type,
        string? referenceType = null,
        Guid? referenceId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        // Create notification entity
        var notification = Notification.CreateForRole(
            role, title, message, type, referenceType, referenceId, actionUrl);

        // Save to database
        await _repository.AddAsync(notification, cancellationToken);

        // Send via SignalR to role group
        var pushDto = notification.Adapt<NotificationPushDto>();
        await _hubContext.Clients.Group(role)
            .SendAsync("ReceiveNotification", pushDto, cancellationToken);

        // Mark as sent
        notification.MarkAsSent();
        await _repository.UpdateAsync(notification, cancellationToken);

        return notification.Id;
    }

    public async Task<Guid> SendBroadcastAsync(
        string title,
        string message,
        NotificationType type,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        // Create notification entity
        var notification = Notification.CreateBroadcast(title, message, type, actionUrl);

        // Save to database
        await _repository.AddAsync(notification, cancellationToken);

        // Send via SignalR to all connected clients
        var pushDto = notification.Adapt<NotificationPushDto>();
        await _hubContext.Clients.All
            .SendAsync("ReceiveNotification", pushDto, cancellationToken);

        // Mark as sent
        notification.MarkAsSent();
        await _repository.UpdateAsync(notification, cancellationToken);

        return notification.Id;
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
            throw new NotFoundException($"Notification {notificationId} not found");

        notification.MarkAsRead();
        await _repository.UpdateAsync(notification, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var spec = new UserUnreadNotificationsSpec(userId);
        var notifications = await _repository.ListAsync(spec, cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        await _repository.UpdateRangeAsync(notifications, cancellationToken);
    }

    public async Task DeleteAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
            throw new NotFoundException($"Notification {notificationId} not found");

        await _repository.DeleteAsync(notification, cancellationToken);
    }

    public async Task<PaginationResponse<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new UserNotificationsSpec(userId, pageNumber, pageSize, isRead);
        var notifications = await _repository.ListAsync(spec, cancellationToken);
        var totalCount = await _repository.CountAsync(spec, cancellationToken);

        var dtos = notifications.Adapt<List<NotificationDto>>();

        return new PaginationResponse<NotificationDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var spec = new UserUnreadNotificationsSpec(userId);
        return await _repository.CountAsync(spec, cancellationToken);
    }
}
