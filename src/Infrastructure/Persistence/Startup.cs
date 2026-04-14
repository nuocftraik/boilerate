using Boilerate.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace Boilerate.Infrastructure.Persistence;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));

    internal static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(nameof(DatabaseSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            _logger.Information("DB Provider: {provider}", dbSettings.DBProvider);
            options.UseDatabase(dbSettings.DBProvider, dbSettings.ConnectionString);
        });
    }

    internal static DbContextOptionsBuilder UseDatabase(
        this DbContextOptionsBuilder builder, 
        string provider, 
        string connectionString)
    {
        return provider.ToLowerInvariant() switch
        {
            "mssql" => builder.UseSqlServer(connectionString, 
                e => e.MigrationsAssembly("Migrators.MSSQL")),
            _ => throw new InvalidOperationException($"DB Provider '{provider}' not supported")
        };
    }
}
