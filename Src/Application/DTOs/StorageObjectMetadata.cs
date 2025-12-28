namespace FileManager.Application.DTOs;

/// <summary>
/// Represents file metadata received from object storage webhooks/events.
/// This DTO is designed to be compatible with MinIO, SeaweedFS, and S3 event notifications.
/// </summary>
/// <param name="Key">The object key (file path) in storage.</param>
/// <param name="Size">The file size in bytes.</param>
/// <param name="ETag">The entity tag (hash) of the file.</param>
/// <param name="ContentType">
/// The MIME type of the file.
/// NOTE: AWS S3 event notifications don't include ContentType - you may need to fetch it separately
/// or use the expected ContentType from your database.
/// MinIO notifications include this field.
/// </param>
/// <param name="LastModified">The last modification timestamp.</param>
/// <param name="VersionId">Optional version ID (for versioned buckets).</param>
/// <param name="Metadata">Optional custom metadata key-value pairs.</param>
/// <remarks>
/// Webhook/Event Compatibility:
/// - MinIO: ✅ Fully compatible (includes all fields)
/// - AWS S3: ⚠️ ContentType not included in S3 events - use database value or fetch separately
/// - SeaweedFS: ⚠️ No built-in events - implement custom webhook with these fields
///
/// For S3, you might need to:
/// 1. Use the ContentType from your database (recommended)
/// 2. Make a HeadObject API call to get ContentType
/// 3. Set ContentType to null and handle in validation logic
/// </remarks>
public sealed record StorageObjectMetadata(
    string Key,
    long Size,
    string ETag,
    string? ContentType,
    DateTime LastModified,
    string? VersionId = null,
    Dictionary<string, string>? Metadata = null
);