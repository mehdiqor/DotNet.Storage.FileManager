namespace FileManager.Common.Options;

/// <summary>
/// Configuration options for S3-compatible storage provider.
/// </summary>
public sealed class S3Options
{
    public const string SectionName = "Storage:S3";

    /// <summary>
    /// S3 service URL (e.g., "https://s3.amazonaws.com" or custom endpoint).
    /// </summary>
    public string ServiceUrl { get; init; } = string.Empty;

    /// <summary>
    /// AWS access key ID.
    /// </summary>
    public string AccessKey { get; init; } = string.Empty;

    /// <summary>
    /// AWS secret access key.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// S3 bucket name for file storage.
    /// </summary>
    public string BucketName { get; init; } = "files";

    /// <summary>
    /// AWS region (e.g., "us-east-1").
    /// </summary>
    public string Region { get; init; } = "us-east-1";

    /// <summary>
    /// Force path-style bucket addressing (for S3-compatible providers).
    /// </summary>
    public bool ForcePathStyle { get; init; } = false;
}