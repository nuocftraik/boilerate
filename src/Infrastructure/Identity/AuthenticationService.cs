using System.Net.Http.Json;
using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Application.Identity.O2Auth;
using Boilerate.Application.Identity.Tokens;
using Boilerate.Domain.Identity;
using Boilerate.Infrastructure.Auth.OAuth2;
using Boilerate.Shared.Authorization;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Boilerate.Infrastructure.Identity;

/// <summary>
/// Authentication service implementation cho OAuth2 social login.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GoogleAuthSettings _googleAuthConfig;
    private readonly ITokenService _tokenService;
    private readonly ISerializerService _serializerService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IOptions<GoogleAuthSettings> googleAuthConfig,
        ITokenService tokenService,
        ISerializerService serializerService,
        IHttpClientFactory httpClientFactory)
    {
        _userManager = userManager;
        _googleAuthConfig = googleAuthConfig.Value;
        _tokenService = tokenService;
        _serializerService = serializerService;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Đăng nhập bằng Google ID Token.
    /// </summary>
    public async Task<TokenResponse> GoogleSignIn(string token, string ipAddress)
    {
        // 1. Validate ID Token với Google
        var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _googleAuthConfig.ClientId }
        });

        // 2. Extract user info từ payload
        var emailLogin = payload.Email;

        // 3. Tìm user trong database
        var existingUser = await _userManager.FindByEmailAsync(emailLogin);

        // 4. Nếu user chưa tồn tại → Tạo user mới
        if (existingUser == null)
        {
            existingUser = new ApplicationUser
            {
                Email = emailLogin,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                UserName = emailLogin,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(existingUser);
            if (!result.Succeeded)
            {
                throw new InternalServerException("Không thể tạo người dùng từ Google");
            }

            await _userManager.AddToRoleAsync(existingUser, AppRoles.Basic);
        }

        // 5. Generate JWT token cho user
        return await _tokenService.GenerateTokensAndUpdateUser(existingUser, ipAddress);
    }

    /// <summary>
    /// Đăng nhập bằng Google Authorization Code.
    /// </summary>
    public async Task<TokenResponse> GoogleSignIn2(string authorizationCode, string ipAddress)
    {
        // 1. Cấu hình Google Authorization Code Flow
        var googleAuthorizationCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _googleAuthConfig.ClientId,
                ClientSecret = _googleAuthConfig.ClientSecret
            },
            Scopes = new[]
            {
                "https://www.googleapis.com/auth/userinfo.profile",
                "https://www.googleapis.com/auth/userinfo.email"
            }
        });

        // 2. Trao đổi Authorization Code để lấy Access Token
        var tokenResponse = await googleAuthorizationCodeFlow.ExchangeCodeForTokenAsync(
            userId: "me",
            code: authorizationCode,
            redirectUri: "postmessage", // Thường là postmessage cho SPA
            CancellationToken.None);

        // 3. Lấy thông tin user từ Google API bằng Access Token
        using var httpClient = _httpClientFactory.CreateClient();
        var userInfoResponse = await httpClient.GetStringAsync(
            $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={tokenResponse.AccessToken}");

        // 4. Deserialize user info response
        var userInfo = _serializerService.Deserialize<GoogleUserInfo>(userInfoResponse);

        // 5. Kiểm tra hoặc tạo user trong database
        var emailLogin = userInfo.Email;
        var existingUser = await _userManager.FindByEmailAsync(emailLogin);

        if (existingUser == null)
        {
            existingUser = new ApplicationUser
            {
                Email = emailLogin,
                FirstName = userInfo.GivenName,
                LastName = userInfo.FamilyName,
                UserName = emailLogin,
                EmailConfirmed = true,
                IsActive = true
            };

            await _userManager.CreateAsync(existingUser);
            await _userManager.AddToRoleAsync(existingUser, AppRoles.Basic);
        }

        // 6. Generate JWT token
        return await _tokenService.GenerateTokensAndUpdateUser(existingUser, ipAddress);
    }

    /// <summary>
    /// Đăng nhập bằng Facebook Access Token.
    /// </summary>
    public async Task<TokenResponse> FacebookSignIn(string accessToken, string ipAddress)
    {
        // 1. Validate Access Token và lấy user info từ Facebook Graph API
        using var httpClient = _httpClientFactory.CreateClient();
        var userInfoUrl = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture&access_token={accessToken}";

        var userInfoResponse = await httpClient.GetAsync(userInfoUrl);

        if (!userInfoResponse.IsSuccessStatusCode)
        {
            throw new UnauthorizedException("Invalid Facebook Access Token");
        }

        // 2. Parse user info
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<FacebookUserInfo>();

        if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
        {
            throw new UnauthorizedException("Unable to get user info from Facebook");
        }

        // 3. Tìm hoặc tạo user trong database
        var emailLogin = userInfo.Email;
        var existingUser = await _userManager.FindByEmailAsync(emailLogin);

        if (existingUser == null)
        {
            existingUser = new ApplicationUser
            {
                Email = emailLogin,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                UserName = emailLogin,
                EmailConfirmed = true,
                IsActive = true,
                ImageUrl = userInfo.Picture?.Data?.Url
            };

            await _userManager.CreateAsync(existingUser);
            await _userManager.AddToRoleAsync(existingUser, AppRoles.Basic);
        }

        // 4. Generate JWT token
        return await _tokenService.GenerateTokensAndUpdateUser(existingUser, ipAddress);
    }
}

/// <summary>
/// Google user info model.
/// </summary>
public class GoogleUserInfo
{
    public string Email { get; set; } = default!;
    public string GivenName { get; set; } = default!;
    public string FamilyName { get; set; } = default!;
    public string Picture { get; set; } = default!;
}

/// <summary>
/// Facebook user info model.
/// </summary>
public class FacebookUserInfo
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public FacebookPicture? Picture { get; set; }
}

public class FacebookPicture
{
    public FacebookPictureData? Data { get; set; }
}

public class FacebookPictureData
{
    public string Url { get; set; } = default!;
}
