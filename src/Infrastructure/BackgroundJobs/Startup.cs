using Boilerate.Application.Common.BackgroundJobs;
using Hangfire;
using Hangfire.Console;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Boilerate.Infrastructure.BackgroundJobs;

internal static class Startup
{
    internal static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<HangfireStorageSettings>(config.GetSection(nameof(HangfireStorageSettings)));
        var settings = config.GetSection(nameof(HangfireStorageSettings)).Get<HangfireStorageSettings>();

        if (settings == null) throw new InvalidOperationException("HangfireStorageSettings is not configured.");

        services.AddHangfire((sp, configuration) =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseConsole(new ConsoleOptions());

            switch (settings.StorageProvider.ToLowerInvariant())
            {
                case "sqlserver":
                    if (string.IsNullOrEmpty(settings.ConnectionString))
                        throw new InvalidOperationException("SQL Server connection string is required for Hangfire storage.");

                    configuration.UseSqlServerStorage(settings.ConnectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        SchemaName = "hangfire"
                    });
                    break;

                case "memory":
                    configuration.UseMemoryStorage();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown Hangfire storage provider: {settings.StorageProvider}");
            }

            configuration.UseFilter(new HangfireJobFilter(sp.GetRequiredService<ILogger<HangfireJobFilter>>()));

            if (settings.EnableAutomaticRetry)
            {
                configuration.UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = settings.MaxRetryAttempts,
                    OnAttemptsExceeded = AttemptsExceededAction.Delete
                });
            }
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = settings.WorkerCount;
            options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
        });

        return services.AddTransient<IJobService, HangfireService>();
    }

    internal static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var settings = config.GetSection(nameof(HangfireStorageSettings)).Get<HangfireStorageSettings>();

        if (settings?.EnableDashboard == true)
        {
            app.UseHangfireDashboard(settings.DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = settings.DashboardTitle,
                StatsPollingInterval = 5000,
                DisplayStorageConnectionString = false
            });
        }

        return app;
    }
}
