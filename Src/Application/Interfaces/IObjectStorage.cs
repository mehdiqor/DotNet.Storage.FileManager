using FileManager.Application.DTOs;
using FileManager.Domain.Enums;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Provider-agnostic interface for object storage operations.
/// Supports MinIO, SeaweedFS, S3-compatible services.
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Gets the storage provider type.
    /// </summary>
    StorageProvider Provider { get; }

    // ============================
    // Upload Operations
    // ============================

    /// <summary>
    /// Uploads a file to object storage.
    /// </summary>
    /// <param name="request">Upload request containing stream, path, and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload result with storage key and ETag.</returns>
    Task<UploadResult> UploadAsync(UploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for client-side direct upload.
    /// </summary>
    /// <param name="request">Presigned upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Presigned URL valid for the specified duration.</returns>
    Task<string> GetPresignedUploadUrlAsync(PresignedUploadRequest request, CancellationToken cancellationToken = default);

    // ============================
    // Download Operations
    // ============================

    /// <summary>
    /// Downloads a file from object storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream containing the file content.</returns>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for client-side direct download.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="expiresIn">URL expiration duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Presigned URL valid for the specified duration.</returns>
    Task<string> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan expiresIn, CancellationToken cancellationToken = default);

    // ============================
    // Remove Operations
    // ============================

    /// <summary>
    /// Removes a single file from object storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple files from object storage in a batch operation.
    /// </summary>
    /// <param name="storageKeys">Collection of storage keys to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveBatchAsync(IEnumerable<string> storageKeys, CancellationToken cancellationToken = default);

    // ============================
    // Metadata Operations
    // ============================

    /// <summary>
    /// Retrieves metadata for a stored object.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Object metadata including size, ETag, and content type.</returns>
    Task<StorageObjectMetadata> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in object storage.
    /// </summary>
    /// <param name="storageKey">The storage key/path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    // ============================
    // Health Check
    // ============================

    /// <summary>
    /// Performs a health check to verify the storage provider is reachable and operational.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the storage provider is healthy, false otherwise.</returns>
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}