using FileManager.Domain.Entities;
using FileManager.Domain.Enums;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Repository interface for FileMetadata entities with domain-specific operations.
/// </summary>
public interface IFileMetadataRepository : IRepository<FileMetadata>
{
    /// <summary>
    /// Gets file metadata by storage key.
    /// </summary>
    Task<FileMetadata?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by hash.
    /// </summary>
    Task<FileMetadata?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files by status.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetByStatusAsync(
        FileStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files by provider.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetByProviderAsync(
        StorageProvider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files uploaded within a date range.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetByUploadDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rejected files with reasons.
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetRejectedFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending files (awaiting validation or scanning).
    /// </summary>
    Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file with the given hash already exists.
    /// </summary>
    Task<bool> ExistsByHashAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file with the given storage key already exists.
    /// </summary>
    Task<bool> ExistsByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);
}
