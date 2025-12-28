using FileManager.Application.DTOs;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Application service for managing file operations.
/// Coordinates between storage providers, repositories, and domain logic.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Uploads a file to storage and creates metadata record.
    /// </summary>
    Task<FileMetadata> UploadFileAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for direct client upload.
    /// </summary>
    Task<string> GeneratePresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    Task<Stream> DownloadFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file by storage key.
    /// </summary>
    Task<Stream> DownloadFileByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned download URL for direct client access.
    /// </summary>
    Task<string> GeneratePresignedDownloadUrlAsync(
        Guid fileId,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by ID.
    /// </summary>
    Task<FileMetadata?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by storage key.
    /// </summary>
    Task<FileMetadata?> GetFileMetadataByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by hash.
    /// </summary>
    Task<FileMetadata?> GetFileMetadataByHashAsync(
        string hash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files by status.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetFilesByStatusAsync(
        FileStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending files awaiting validation or scanning.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from both storage and database.
    /// </summary>
    Task DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple files.
    /// </summary>
    Task DeleteFilesAsync(
        IEnumerable<Guid> fileIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an uploaded file by comparing actual storage metadata (from webhook/event) with expected metadata in database.
    /// This method should be called when object storage webhook fires after file upload.
    /// If validation passes and virus scanning is DISABLED, file status transitions to Available.
    /// If validation passes and virus scanning is ENABLED, file status transitions to Uploaded (awaiting scan).
    /// If validation fails, the file is DELETED from storage and status is set to Rejected.
    /// </summary>
    /// <param name="storageKey">The storage key of the file received from object storage webhook.</param>
    /// <param name="actualMetadata">The actual file metadata received from object storage webhook/event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Typical workflow:
    /// 1. File is uploaded to object storage
    /// 2. Object storage triggers webhook with storage key and actual file metadata (size, content type, etc.)
    /// 3. Webhook handler calls this method with the storage key and received metadata
    /// 4. Method looks up file in database by storage key
    /// 5. If actualMetadata is incomplete (e.g., ContentType is null for S3), method automatically fetches
    ///    complete metadata from storage using GetMetadataAsync
    /// 6. Method compares actual metadata with database metadata
    ///
    /// The validation logic:
    /// - Compares actual file size with expected size from database
    /// - Compares actual content type with expected type from database
    /// - Validates file size is within configured limits (MaxFileSizeBytes)
    ///
    /// Automatic metadata fetching (S3/SeaweedFS compatibility):
    /// - If ContentType is null in webhook data, SDK automatically calls GetMetadataAsync
    /// - This ensures validation has complete data even for S3 events (which don't include ContentType)
    /// - No additional code needed in webhook handler
    ///
    /// Status transitions:
    /// - If validation passes AND virus scanning is disabled → status = Available
    /// - If validation passes AND virus scanning is enabled → status = Uploaded (awaiting scan)
    /// - If validation fails → status = Rejected + file DELETED from storage
    ///
    /// After validation, domain events are published:
    /// - FileValidatedEvent (on success)
    /// - FileRejectedEvent (on failure)
    /// </remarks>
    Task ValidateFileAsync(
        string storageKey,
        StorageObjectMetadata actualMetadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists by ID.
    /// </summary>
    Task<bool> FileExistsAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file with the given hash already exists.
    /// </summary>
    Task<bool> FileExistsByHashAsync(
        string hash,
        CancellationToken cancellationToken = default);
}