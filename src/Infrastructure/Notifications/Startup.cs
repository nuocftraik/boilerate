using Boilerate.Infrastructure.Notifications.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerate.Infrastructure.Notifications;

public static class Startup
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration config)
    {
        // Add SignalR
        var signalRBuilder = services.AddSignalR();

        // Add Redis backplane for scaling (optional)
        var redisConnection = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
            {
                options.Configuration.ChannelPrefix = "Boilerate.Notifications";
            });
        }

        return services;
    }

    public static IEndpointRouteBuilder MapNotifications(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
        return endpoints;
    }
}
