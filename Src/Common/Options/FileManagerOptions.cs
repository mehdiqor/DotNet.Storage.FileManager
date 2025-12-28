using FileManager.Domain.Enums;

namespace FileManager.Common.Options;

/// <summary>
/// General configuration options for the File Manager SDK.
/// </summary>
public sealed class FileManagerOptions
{
    public const string SectionName = "FileManager";

    /// <summary>
    /// The default storage provider to use (MinIO, SeaweedFS, or S3).
    /// </summary>
    public StorageProvider DefaultProvider { get; init; } = StorageProvider.MinIo;

    /// <summary>
    /// Enable file validation after upload.
    /// </summary>
    public bool ValidationEnabled { get; init; }

    /// <summary>
    /// Enable virus scanning for uploaded files.
    /// </summary>
    public bool VirusScanningEnabled { get; init; }

    /// <summary>
    /// Default expiration time for presigned URLs.
    /// </summary>
    public TimeSpan PresignedUrlExpiration { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum file size in bytes (default 5GB).
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = 5L * 1024 * 1024 * 1024;
}