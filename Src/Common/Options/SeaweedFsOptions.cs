namespace FileManager.Common.Options;

/// <summary>
/// Configuration options for SeaweedFS storage provider.
/// </summary>
public sealed class SeaweedFsOptions
{
    public const string SectionName = "Storage:SeaweedFS";

    /// <summary>
    /// SeaweedFS Filer URL (e.g., "http://localhost:8888").
    /// </summary>
    public string FilerUrl { get; init; } = string.Empty;

    /// <summary>
    /// Bucket/collection name for file storage.
    /// </summary>
    public string BucketName { get; init; } = "files";

    /// <summary>
    /// Secret key for HMAC-signed URLs (optional).
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// Use direct URLs instead of presigned URLs.
    /// </summary>
    public bool UseDirectUrls { get; init; } = false;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}