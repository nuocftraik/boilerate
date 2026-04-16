using Boilerate.Application.Identity.O2Auth;
using Boilerate.Host.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Boilerate.Host.Controllers.Identity;

/// <summary>
/// Authentication controller cho OAuth2 social login.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Đăng nhập bằng Google ID Token.
    /// </summary>
    /// <param name="request">Request chứa ID Token.</param>
    /// <returns>Token response.</returns>
    [HttpPost("google")]
    [AllowAnonymous]
    [OpenApiOperation("Đăng nhập bằng Google ID Token", "")]
    public async Task<IActionResult> GoogleLogin([FromBody] OAuthRequest request)
    {
        var response = await _authenticationService.GoogleSignIn(request.IdToken, GetIpAddress()!);
        return Ok(response);
    }

    /// <summary>
    /// Đăng nhập bằng Google Authorization Code.
    /// </summary>
    /// <param name="authorizedCode">Code nhận từ Google.</param>
    /// <returns>Token response.</returns>
    [HttpPost("google2")]
    [AllowAnonymous]
    [OpenApiOperation("Đăng nhập bằng Google Authorization Code", "")]
    public async Task<IActionResult> GoogleLogin2([FromBody] string authorizedCode)
    {
        var response = await _authenticationService.GoogleSignIn2(authorizedCode, GetIpAddress()!);
        return Ok(response);
    }

    /// <summary>
    /// Đăng nhập bằng Facebook Access Token.
    /// </summary>
    /// <param name="request">Request chứa Access Token.</param>
    /// <returns>Token response.</returns>
    [HttpPost("facebook")]
    [AllowAnonymous]
    [OpenApiOperation("Đăng nhập bằng Facebook Access Token", "")]
    public async Task<IActionResult> FacebookLogin([FromBody] OAuthRequest request)
    {
        var response = await _authenticationService.FacebookSignIn(request.IdToken, GetIpAddress()!);
        return Ok(response);
    }

    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}
