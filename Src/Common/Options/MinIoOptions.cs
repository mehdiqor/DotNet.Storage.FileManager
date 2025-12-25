namespace FileManager.Common.Options;

/// <summary>
/// Configuration options for MinIO storage provider.
/// </summary>
public sealed class MinIoOptions
{
    public const string SectionName = "Storage:MinIO";

    /// <summary>
    /// MinIO server endpoint (e.g., "localhost:9000").
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Access key for authentication.
    /// </summary>
    public string AccessKey { get; init; } = string.Empty;

    /// <summary>
    /// Secret key for authentication.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Bucket name for file storage.
    /// </summary>
    public string BucketName { get; init; } = "files";

    /// <summary>
    /// Use SSL/TLS for connection.
    /// </summary>
    public bool UseSsl { get; init; } = false;

    /// <summary>
    /// Optional region for the bucket.
    /// </summary>
    public string? Region { get; init; }
}