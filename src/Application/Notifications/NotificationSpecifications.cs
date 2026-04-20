using Ardalis.Specification;
using Boilerate.Domain.Notifications;

namespace Boilerate.Application.Notifications;

/// <summary>
/// Specification to get user notifications with pagination
/// </summary>
public class UserNotificationsSpec : Specification<Notification>
{
    public UserNotificationsSpec(Guid userId, int pageNumber, int pageSize, bool? isRead = null)
    {
        Query
            .Where(n => n.UserId == userId || n.TargetRole != null || (n.UserId == null && n.TargetRole == null))
            .OrderByDescending(n => n.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        if (isRead.HasValue)
        {
            Query.Where(n => n.IsRead == isRead.Value);
        }
    }
}

/// <summary>
/// Specification to get unread notifications for user
/// </summary>
public class UserUnreadNotificationsSpec : Specification<Notification>
{
    public UserUnreadNotificationsSpec(Guid userId)
    {
        Query.Where(n =>
            !n.IsRead &&
            (n.UserId == userId || n.TargetRole != null || (n.UserId == null && n.TargetRole == null)));
    }
}
