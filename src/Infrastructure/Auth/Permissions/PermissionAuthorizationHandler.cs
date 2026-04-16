using Boilerate.Application.Identity.Users;
using Boilerate.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Boilerate.Infrastructure.Auth.Permissions;

/// <summary>
/// Handler kiểm tra xem user có permission yêu cầu không
/// Check trong JWT claims trước, nếu không thấy thì check database (via IUserService)
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserService _userService;

    public PermissionAuthorizationHandler(IUserService userService)
    {
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // 1. Check permissions trong JWT claims (fast path)
        var permissions = context.User.Claims
            .Where(x => x.Type == AppClaims.Permission)
            .Select(x => x.Value)
            .ToList();

        if (permissions.Any(x => x == requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Nếu không tìm thấy trong claims, check database (cho phép update permission ngay lập tức không cần login lại)
        // Lưu ý: Cần cân đối hiệu năng nếu check DB mọi request
        if (await _userService.HasPermissionAsync(userId, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
