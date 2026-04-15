using Boilerate.Application.Common.Interfaces;
using Boilerate.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Boilerate.Infrastructure.Persistence.Initialization;

internal class ApplicationDbInitializer : ITransientService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ApplicationDbInitializer> _logger;
    private readonly ApplicationDbSeeder _dbSeeder;

    public ApplicationDbInitializer(
        ApplicationDbContext dbContext,
        ILogger<ApplicationDbInitializer> logger,
        ApplicationDbSeeder dbSeeder)
    {
        _dbContext = dbContext;
        _logger = logger;
        _dbSeeder = dbSeeder;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.GetMigrations().Any())
        {
            if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                _logger.LogInformation("Applying Migrations for system");
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }

            if (await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Connection to system's Database Succeeded.");
                await _dbSeeder.SeedDatabaseAsync(_dbContext, cancellationToken);
            }
        }
    }
}
