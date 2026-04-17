using Boilerate.Application.Common.BlobStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Host.Controllers.Common;

/// <summary>
/// Blob storage testing endpoints (Development only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // For testing only - remove in production
public class BlobStorageController : BaseApiController
{
    private readonly IBlobStorageService _blobStorage;

    public BlobStorageController(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Upload file to blob storage
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] string containerName,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var request = new UploadBlobRequest
        {
            ContainerName = containerName,
            BlobName = blobName,
            ContentType = file.ContentType,
            Data = file.OpenReadStream(),
            Metadata = new Dictionary<string, string>
            {
                { "OriginalFileName", file.FileName },
                { "UploadedAt", DateTime.UtcNow.ToString("O") }
            }
        };

        var url = await _blobStorage.UploadAsync(request, cancellationToken);

        return Ok(new
        {
            url,
            containerName,
            blobName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    /// <summary>
    /// Download file from blob storage
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> Download(
        [FromQuery] string containerName,
        [FromQuery] string blobName,
        CancellationToken cancellationToken = default)
    {
        var stream = await _blobStorage.DownloadAsync(containerName, blobName, cancellationToken);

        var contentType = Path.GetExtension(blobName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        return File(stream, contentType, blobName);
    }

    /// <summary>
    /// Delete blob from storage
    /// </summary>
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(
        [FromQuery] string containerName,
        [FromQuery] string blobName,
        CancellationToken cancellationToken = default)
    {
        await _blobStorage.DeleteAsync(containerName, blobName, cancellationToken);

        return Ok(new { message = $"Blob '{blobName}' deleted successfully." });
    }

    /// <summary>
    /// Get temporary download link (SAS token)
    /// </summary>
    [HttpGet("download-link")]
    public async Task<IActionResult> GetDownloadLink(
        [FromQuery] string containerName,
        [FromQuery] string blobName,
        [FromQuery] int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var url = await _blobStorage.GetBlobUrlAsync(
            containerName,
            blobName,
            expiryMinutes,
            cancellationToken);

        return Ok(new
        {
            url,
            expiresIn = $"{expiryMinutes} minutes"
        });
    }

    /// <summary>
    /// List blobs in container
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListBlobs(
        [FromQuery] string containerName,
        [FromQuery] string? prefix,
        CancellationToken cancellationToken = default)
    {
        var blobs = await _blobStorage.ListBlobsAsync(containerName, prefix, cancellationToken);

        return Ok(new
        {
            containerName,
            prefix = prefix ?? "(none)",
            count = blobs.Count,
            blobs
        });
    }

    /// <summary>
    /// Create container
    /// </summary>
    [HttpPost("containers")]
    public async Task<IActionResult> CreateContainer(
        [FromQuery] string containerName,
        [FromQuery] bool isPublic = false,
        CancellationToken cancellationToken = default)
    {
        await _blobStorage.CreateContainerAsync(containerName, isPublic, cancellationToken);

        return Ok(new
        {
            message = $"Container '{containerName}' created successfully.",
            isPublic
        });
    }

    /// <summary>
    /// List all containers
    /// </summary>
    [HttpGet("containers")]
    public async Task<IActionResult> ListContainers(CancellationToken cancellationToken = default)
    {
        var containers = await _blobStorage.ListContainersAsync(cancellationToken);

        return Ok(new
        {
            count = containers.Count,
            containers
        });
    }
}
