using Boilerate.Domain.Identity;
using Boilerate.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Identity;

internal partial class UserService
{
    /// <summary>
    /// Lấy danh sách tất cả permissions của user từ database (vượt qua roles)
    /// </summary>
    public async Task<List<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        // 1. Lấy danh sách Role IDs của user
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        var userRoles = await _userManager.GetRolesAsync(user);
        var roleIds = await _db.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (!roleIds.Any()) return new List<string>();

        // 2. Query permissions table join với Function và Action
        var permissions = await _db.Permissions
            .AsNoTracking()
            .Include(p => p.Function)
            .Include(p => p.Action)
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => AppPermission.NameFor(p.Action.Name, p.Function.Name))
            .ToListAsync(cancellationToken);

        // Trả về danh sách duy nhất, loại bỏ "Permissions." prefix nếu cần (nhưng handler mong đợi full name)
        // Lưu ý: AppPermission.NameFor trả về "Permissions.Function.Action"
        return permissions.Distinct().ToList();
    }

    /// <summary>
    /// Kiểm tra xem user có một permission cụ thể không
    /// </summary>
    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        // Tối ưu: Lấy list và check (hoặc query trực tiếp nếu cần performance cực cao)
        // Ở đây query trực tiếp để tối ưu performance cho handler
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var userRoles = await _userManager.GetRolesAsync(user);

        // Admin bypass
        if (userRoles.Contains(AppRoles.Admin)) return true;

        var roleIds = await _db.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);


        if (!roleIds.Any()) return false;

        // Check xem có permission record nào match không
        return await _db.Permissions
            .AsNoTracking()
            .Include(p => p.Function)
            .Include(p => p.Action)
            .AnyAsync(p =>
                roleIds.Contains(p.RoleId) &&
                $"Permissions.{p.Function.Name}.{p.Action.Name}" == permission,
                cancellationToken);
    }
}
