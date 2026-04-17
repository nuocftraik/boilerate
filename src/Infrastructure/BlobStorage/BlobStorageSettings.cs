namespace Boilerate.Infrastructure.BlobStorage;

/// <summary>
/// Configuration for Azure Blob Storage
/// </summary>
public class BlobStorageSettings
{
    /// <summary>
    /// Azure Blob Storage connection string
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// Default container name
    /// </summary>
    public string DefaultContainer { get; set; } = "default";

    /// <summary>
    /// Enable public access by default
    /// </summary>
    public bool DefaultPublicAccess { get; set; } = false;
}
