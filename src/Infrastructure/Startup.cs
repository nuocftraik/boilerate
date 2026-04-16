using Boilerate.Infrastructure.Auth;
using Boilerate.Infrastructure.BackgroundJobs;
using Boilerate.Infrastructure.Caching;
using Boilerate.Infrastructure.Common;
using Boilerate.Infrastructure.FileStorage;
using Boilerate.Infrastructure.Identity;
using Boilerate.Infrastructure.Mailing;
using Boilerate.Infrastructure.Middleware;
using Boilerate.Infrastructure.Persistence;
using Boilerate.Infrastructure.Persistence.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerate.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        return services
            .AddPersistence()
            .AddIdentity()
            .AddAuth()
            .AddCaching(config)
            .AddMailing(config)
            .AddBackgroundJobs(config)
            .AddExceptionMiddleware()
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices();
    }

    public static IApplicationBuilder UseInfrastructure(
        this IApplicationBuilder builder,
        IConfiguration config)
    {
        return builder
            .UseExceptionMiddleware()
            .UseRouting()
            .UseFileStorage()
            .UseAuth()
            .UseHangfireDashboard(config);
    }


    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers();
        return builder;
    }

    /// <summary>
    /// Khởi tạo Database: apply migrations và seed dữ liệu nền tảng.
    /// Gọi từ Program.cs sau khi build app.
    /// </summary>
    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }
}
