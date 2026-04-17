using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Boilerate.Application.Common.BlobStorage;
using Boilerate.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Infrastructure.BlobStorage;

/// <summary>
/// Azure Blob Storage implementation
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<AzureBlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        }

        _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
    }

    public async Task<string> UploadAsync(UploadBlobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(request.ContainerName);

            // Create container if not exists
            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: _settings.DefaultPublicAccess
                    ? PublicAccessType.Blob
                    : PublicAccessType.None,
                cancellationToken: cancellationToken);

            // Get blob client
            var blobClient = containerClient.GetBlobClient(request.BlobName);

            // Check if blob exists và overwrite setting
            if (!request.Overwrite && await blobClient.ExistsAsync(cancellationToken))
            {
                throw new ConflictException($"Blob '{request.BlobName}' already exists in container '{request.ContainerName}'.");
            }

            // Upload options
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = request.ContentType
                }
            };

            // Add metadata if provided
            if (request.Metadata != null && request.Metadata.Any())
            {
                uploadOptions.Metadata = request.Metadata;
            }

            // Upload blob
            await blobClient.UploadAsync(
                request.Data,
                uploadOptions,
                cancellationToken);

            _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}' ({Size} bytes)",
                request.BlobName,
                request.ContainerName,
                request.Data.Length);

            // Return blob URL
            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upload blob '{BlobName}' to container '{ContainerName}'",
                request.BlobName, request.ContainerName);
            throw new InternalServerException($"Failed to upload blob: {ex.Message}");
        }
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            // Download to memory stream
            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0; // Reset position for reading

            _logger.LogInformation(
                "Downloaded blob '{BlobName}' from container '{ContainerName}' ({Size} bytes)",
                blobName,
                containerName,
                memoryStream.Length);

            return memoryStream;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob '{BlobName}' from container '{ContainerName}'",
                blobName, containerName);
            throw new InternalServerException($"Failed to download blob: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            // Delete blob
            var deleted = await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            if (deleted.Value)
            {
                _logger.LogInformation(
                    "Deleted blob '{BlobName}' from container '{ContainerName}'",
                    blobName,
                    containerName);
            }
            else
            {
                _logger.LogWarning(
                    "Blob '{BlobName}' not found in container '{ContainerName}' (already deleted?)",
                    blobName,
                    containerName);
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete blob '{BlobName}' from container '{ContainerName}'",
                blobName, containerName);
            throw new InternalServerException($"Failed to delete blob: {ex.Message}");
        }
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob '{BlobName}' in container '{ContainerName}'",
                blobName, containerName);
            return false;
        }
    }

    public async Task<string> GetBlobUrlAsync(
        string containerName,
        string blobName,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            // Check if container is public
            var properties = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            if (properties.Value.PublicAccess != PublicAccessType.None)
            {
                // Public container - return direct URL
                return blobClient.Uri.ToString();
            }

            // Private container - generate SAS token
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Cannot generate SAS token. Check storage account configuration.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b", // Blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Grace period
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            // Grant read permission
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate SAS token and URL
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get URL for blob '{BlobName}' in container '{ContainerName}'",
                blobName, containerName);
            throw new InternalServerException($"Failed to get blob URL: {ex.Message}");
        }
    }

    public async Task<List<BlobModel>> ListBlobsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Check if container exists
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Container '{containerName}' not found.");
            }

            var blobs = new List<BlobModel>();

            // List blobs with optional prefix filter
            await foreach (var blobItem in containerClient.GetBlobsAsync(
                prefix: prefix,
                cancellationToken: cancellationToken))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);

                blobs.Add(new BlobModel
                {
                    Name = blobItem.Name,
                    ContainerName = containerName,
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType ?? "application/octet-stream",
                    LastModified = blobItem.Properties.LastModified ?? DateTimeOffset.UtcNow,
                    Url = blobClient.Uri.ToString(),
                    ETag = blobItem.Properties.ETag?.ToString(),
                    Metadata = blobItem.Metadata
                });
            }

            _logger.LogInformation(
                "Listed {Count} blobs in container '{ContainerName}' (prefix: '{Prefix}')",
                blobs.Count,
                containerName,
                prefix ?? "(none)");

            return blobs;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list blobs in container '{ContainerName}'", containerName);
            throw new InternalServerException($"Failed to list blobs: {ex.Message}");
        }
    }

    public async Task CreateContainerAsync(
        string containerName,
        bool isPublic = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Create container
            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: isPublic ? PublicAccessType.Blob : PublicAccessType.None,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created container '{ContainerName}' (public: {IsPublic})",
                containerName,
                isPublic);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to create container '{ContainerName}'", containerName);
            throw new InternalServerException($"Failed to create container: {ex.Message}");
        }
    }

    public async Task DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Delete container
            var deleted = await containerClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (deleted.Value)
            {
                _logger.LogInformation("Deleted container '{ContainerName}'", containerName);
            }
            else
            {
                _logger.LogWarning("Container '{ContainerName}' not found (already deleted?)", containerName);
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete container '{ContainerName}'", containerName);
            throw new InternalServerException($"Failed to delete container: {ex.Message}");
        }
    }

    public async Task<List<string>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = new List<string>();

            await foreach (var containerItem in _blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
            {
                containers.Add(containerItem.Name);
            }

            _logger.LogInformation("Listed {Count} containers", containers.Count);

            return containers;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list containers");
            throw new InternalServerException($"Failed to list containers: {ex.Message}");
        }
    }
}
