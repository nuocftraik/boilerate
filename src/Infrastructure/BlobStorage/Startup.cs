using Boilerate.Application.Common.BlobStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Boilerate.Infrastructure.BlobStorage;

/// <summary>
/// Blob storage dependency injection registration
/// </summary>
internal static class Startup
{
    internal static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Configure settings
        services.Configure<BlobStorageSettings>(
            config.GetSection(nameof(BlobStorageSettings)));

        var settings = config.GetSection(nameof(BlobStorageSettings)).Get<BlobStorageSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException("BlobStorageSettings is not configured.");
        }

        if (string.IsNullOrEmpty(settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        }

        // Register Azure Blob Storage service
        services.AddTransient<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}
