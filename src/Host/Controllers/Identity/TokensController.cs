using Boilerate.Application.Identity.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Boilerate.Host.Controllers.Identity;

/// <summary>
/// Token management APIs
/// </summary>
public sealed class TokensController : BaseApiController
{
    private readonly ITokenService _tokenService;

    public TokensController(ITokenService tokenService) => _tokenService = tokenService;

    /// <summary>
    /// Login và lấy access token
    /// </summary>
    /// <param name="request">Email và Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TokenResponse chứa access token và refresh token</returns>
    [HttpPost("get")]
    [AllowAnonymous]
    [OpenApiOperation("Request an access token using credentials.", "")]
    public Task<TokenResponse> GetTokenAsync(
        TokenRequest request,
        CancellationToken cancellationToken)
    {
        return _tokenService.GetTokenAsync(request, GetIpAddress()!, cancellationToken);
    }

    /// <summary>
    /// Refresh access token bằng refresh token
    /// </summary>
    /// <param name="request">Expired access token và refresh token</param>
    /// <returns>TokenResponse chứa tokens mới</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [OpenApiOperation("Request an access token using a refresh token.", "")]
    public Task<TokenResponse> RefreshAsync(RefreshTokenRequest request)
    {
        return _tokenService.RefreshTokenAsync(request, GetIpAddress()!);
    }

    /// <summary>
    /// Get client IP address (support proxy)
    /// </summary>
    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}
