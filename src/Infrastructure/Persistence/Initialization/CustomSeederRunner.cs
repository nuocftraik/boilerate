using Boilerate.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerate.Infrastructure.Persistence.Initialization;

internal class CustomSeederRunner : ITransientService
{
    private readonly ICustomSeeder[] _seeders;

    public CustomSeederRunner(IServiceProvider serviceProvider) =>
        _seeders = serviceProvider.GetServices<ICustomSeeder>().ToArray();

    public async Task RunSeedersAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seeders)
        {
            await seeder.InitializeAsync(cancellationToken);
        }
    }
}
