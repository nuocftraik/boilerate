namespace Boilerate.Application.Identity.Users;

/// <summary>
/// Request để assign roles to user
/// </summary>
public class UserRolesRequest
{
    /// <summary>
    /// List of roles với Enabled flags
    /// </summary>
    public List<UserRoleDto> UserRoles { get; set; } = default!;
}
