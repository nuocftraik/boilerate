using Amazon.S3;
using Boilerate.Application.Common.BlobStorage;
using Boilerate.Infrastructure.BlobStorage.Azure;
using Boilerate.Infrastructure.BlobStorage.Aws;
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
        // Configure root settings
        services.Configure<BlobStorageSettings>(
            config.GetSection(nameof(BlobStorageSettings)));

        var settings = config.GetSection(nameof(BlobStorageSettings)).Get<BlobStorageSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException("BlobStorageSettings is not configured.");
        }

        // Register based on provider
        switch (settings.Provider.ToLowerInvariant())
        {
            case "azure":
                RegisterAzureBlobStorage(services, settings);
                break;

            case "aws":
                RegisterAwsS3Storage(services, settings);
                break;

            case "local":
                throw new NotImplementedException("Local blob storage not implemented yet. Use Azure or AWS.");

            default:
                throw new InvalidOperationException($"Unknown blob storage provider: {settings.Provider}");
        }

        return services;
    }

    private static void RegisterAzureBlobStorage(IServiceCollection services, BlobStorageSettings settings)
    {
        if (settings.Azure == null || string.IsNullOrEmpty(settings.Azure.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        }

        services.AddTransient<IBlobStorageService, AzureBlobStorageService>();
    }

    private static void RegisterAwsS3Storage(IServiceCollection services, BlobStorageSettings settings)
    {
        if (settings.Aws == null)
        {
            throw new InvalidOperationException("AWS S3 settings are not configured.");
        }

        // Register AWS S3 client
        services.AddAWSService<IAmazonS3>();

        // Configure AWS options
        services.AddDefaultAWSOptions(
            new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName(settings.Aws.Region),
                Credentials = new Amazon.Runtime.BasicAWSCredentials(settings.Aws.AccessKey, settings.Aws.SecretKey)
            });

        services.AddTransient<IBlobStorageService, AwsS3StorageService>();
    }
}
