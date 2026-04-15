using Boilerate.Infrastructure.Common;
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
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices();
    }

    public static IApplicationBuilder UseInfrastructure(
        this IApplicationBuilder builder,
        IConfiguration config)
    {
        return builder
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization();
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
