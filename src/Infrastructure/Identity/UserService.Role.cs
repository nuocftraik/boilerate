using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Identity.Users;
using Boilerate.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        _ = user ?? throw new NotFoundException("User Not Found.");

        var userRoles = await _userManager.GetRolesAsync(user);

        var allRoles = await _roleManager.Roles.ToListAsync(cancellationToken);

        var roleDtos = allRoles.Select(role => new UserRoleDto
        {
            RoleId = role.Id,
            RoleName = role.Name!,
            Description = role.Description,
            Enabled = userRoles.Contains(role.Name!)
        }).ToList();

        return roleDtos;
    }

    public async Task<string> AssignRolesAsync(string userId, UserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        _ = user ?? throw new NotFoundException("User Not Found.");

        // Check if we are trying to remove Admin role from the last admin
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) 
            && request.UserRoles.Any(a => a.RoleName == AppRoles.Admin && !a.Enabled))
        {
            // Count admins
            int adminCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;
            if (adminCount <= 1)
            {
                throw new ConflictException("Cannot remove Admin role from the last administrator account.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!result.Succeeded)
        {
            throw new InternalServerException("Update roles failed", result.GetErrors());
        }

        var rolesToAdd = request.UserRoles.Where(x => x.Enabled).Select(x => x.RoleName!).ToList();
        result = await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (!result.Succeeded)
        {
            throw new InternalServerException("Update roles failed", result.GetErrors());
        }

        return "User Roles Updated Successfully.";
    }
}
