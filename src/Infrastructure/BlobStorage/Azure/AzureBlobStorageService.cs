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

namespace Boilerate.Infrastructure.BlobStorage.Azure;

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

        if (_settings.Azure == null || string.IsNullOrEmpty(_settings.Azure.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        }

        _blobServiceClient = new BlobServiceClient(_settings.Azure.ConnectionString);
    }

    public async Task<string> UploadAsync(UploadBlobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(request.ContainerName);

            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: _settings.DefaultPublicAccess
                    ? PublicAccessType.Blob
                    : PublicAccessType.None,
                cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(request.BlobName);

            if (!request.Overwrite && await blobClient.ExistsAsync(cancellationToken))
            {
                throw new ConflictException($"Blob '{request.BlobName}' already exists in container '{request.ContainerName}'.");
            }

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType }
            };

            if (request.Metadata != null && request.Metadata.Any())
            {
                uploadOptions.Metadata = request.Metadata;
            }

            await blobClient.UploadAsync(request.Data, uploadOptions, cancellationToken);

            _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}'", request.BlobName, request.ContainerName);

            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upload blob '{BlobName}' to container '{ContainerName}'", request.BlobName, request.ContainerName);
            throw new InternalServerException($"Failed to upload blob: {ex.Message}");
        }
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            throw new InternalServerException($"Failed to download blob: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            throw new InternalServerException($"Failed to delete blob: {ex.Message}");
        }
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            return false;
        }
    }

    public async Task<string> GetBlobUrlAsync(string containerName, string blobName, int expiryMinutes = 60, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            var properties = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            if (properties.Value.PublicAccess != PublicAccessType.None)
            {
                return blobClient.Uri.ToString();
            }

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Cannot generate SAS token.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get URL for blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            throw new InternalServerException($"Failed to get blob URL: {ex.Message}");
        }
    }

    public async Task<List<BlobModel>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                throw new NotFoundException($"Container '{containerName}' not found.");
            }

            var blobs = new List<BlobModel>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
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
            return blobs;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list blobs in container '{ContainerName}'", containerName);
            throw new InternalServerException($"Failed to list blobs: {ex.Message}");
        }
    }

    public async Task CreateContainerAsync(string containerName, bool isPublic = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: isPublic ? PublicAccessType.Blob : PublicAccessType.None,
                cancellationToken: cancellationToken);
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
            await containerClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
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
            return containers;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list containers");
            throw new InternalServerException($"Failed to list containers: {ex.Message}");
        }
    }
}
