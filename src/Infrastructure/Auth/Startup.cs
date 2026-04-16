using Boilerate.Application.Common.Interfaces;
using Boilerate.Infrastructure.Auth.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerate.Infrastructure.Auth;

internal static class Startup
{
    /// <summary>
    /// Register CurrentUser services.
    /// </summary>
    internal static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOptions<SecuritySettings>()
            .BindConfiguration(nameof(SecuritySettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register middleware as Scoped (per request)
        services.AddScoped<CurrentUserMiddleware>();

        // Register CurrentUser as Scoped - mỗi request một instance
        // Cả 2 interfaces đều resolve về cùng instance
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentUserInitializer, CurrentUser>();

        return services
            .AddPermissions()
            .AddJwtAuth();
    }

    /// <summary>
    /// Register dynamic permission-based authorization.
    /// </summary>
    private static IServiceCollection AddPermissions(this IServiceCollection services) =>
        services
            .AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Permissions.PermissionPolicyProvider>()
            .AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Permissions.PermissionAuthorizationHandler>();


    internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();

    internal static IApplicationBuilder UseAuth(this IApplicationBuilder app) =>
        app
            .UseCurrentUser()
            .UseAuthentication()
            .UseAuthorization();
}
