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
    /// Marks a file as validated.
    /// </summary>
    Task MarkAsValidatedAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a file as scanned (virus scan complete).
    /// </summary>
    Task MarkAsScannedAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a file with a reason.
    /// </summary>
    Task RejectFileAsync(
        Guid fileId,
        string reason,
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