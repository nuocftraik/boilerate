using Boilerate.Application.Identity.Users;
using Boilerate.Application.Identity.Users.Password;
using Boilerate.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Boilerate.Host.Controllers.Personal;

/// <summary>
/// Personal Controller - Current user profile management
/// Endpoints: Profile, Change password, Get permissions
/// </summary>
[Authorize]
public class PersonalController : BaseApiController
{

    private readonly IUserService _userService;

    public PersonalController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lấy profile của current logged-in user
    /// </summary>
    [HttpGet("profile")]
    [OpenApiOperation("Get profile details of currently logged in user.", "")]
    public async Task<ActionResult<UserDetailDto>> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        // Get user ID từ JWT claims (extension method)
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var profile = await _userService.GetAsync(userId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Cập nhật profile của current logged-in user
    /// </summary>
    [HttpPut("profile")]
    [OpenApiOperation("Update profile details of currently logged in user.", "")]
    public async Task<ActionResult> UpdateProfileAsync(UpdateUserRequest request)
    {
        // Get user ID từ JWT claims
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        await _userService.UpdateAsync(request, userId);
        return Ok(new { message = "Profile updated successfully" });
    }

    /// <summary>
    /// Đổi password của current logged-in user
    /// </summary>
    [HttpPut("change-password")]
    [OpenApiOperation("Change password of currently logged in user.", "")]
    public async Task<ActionResult> ChangePasswordAsync(ChangePasswordRequest model)
    {
        // Get user ID từ JWT claims
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        await _userService.ChangePasswordAsync(model, userId);
        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Lấy danh sách permissions của current logged-in user
    /// </summary>
    [HttpGet("permissions")]
    [OpenApiOperation("Get permissions of currently logged in user.", "")]
    public async Task<ActionResult<List<string>>> GetPermissionsAsync(
        CancellationToken cancellationToken)
    {
        // Get user ID từ JWT claims
        var userId = User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var permissions = await _userService.GetPermissionsAsync(userId, cancellationToken);
        return Ok(permissions);
    }
}
