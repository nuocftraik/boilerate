using Boilerate.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerate.Infrastructure.Common;

internal static class Startup
{
    internal static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddServices(typeof(ITransientService), ServiceLifetime.Transient)
            .AddServices(typeof(IScopedService), ServiceLifetime.Scoped)
            .AddServices(typeof(ISingletonService), ServiceLifetime.Singleton);

    internal static IServiceCollection AddServices(this IServiceCollection services, Type interfaceType, ServiceLifetime lifetime)
    {
        var interfaceTypes =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => interfaceType.IsAssignableFrom(t)
                            && t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Implementation = t,
                    Interfaces = t.GetInterfaces().Where(i => i != interfaceType && i != typeof(ITransientService) && i != typeof(IScopedService) && i != typeof(ISingletonService)).ToList()
                })
                .Select(t => new
                {
                    Service = t.Interfaces.FirstOrDefault() ?? t.Implementation,
                    Implementation = t.Implementation
                });

        foreach (var type in interfaceTypes)
        {
            services.AddService(type.Service!, type.Implementation, lifetime);
        }

        return services;
    }

    internal static IServiceCollection AddService(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
            ServiceLifetime.Scoped => services.AddScoped(serviceType, implementationType),
            ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
            _ => throw new ArgumentException("Invalid lifetime", nameof(lifetime))
        };
}
