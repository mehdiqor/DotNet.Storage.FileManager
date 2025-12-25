using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for FileMetadata entities with domain-specific queries.
/// </summary>
internal sealed class FileMetadataRepository(FileManagerDbContext context)
    : Repository<FileMetadata>(context), IFileMetadataRepository
{
    public async Task<FileMetadata?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.StorageKey == storageKey, cancellationToken);
    }

    public async Task<FileMetadata?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.Hash == hash, cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetByStatusAsync(
        FileStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.Status == status)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetByProviderAsync(
        StorageProvider provider,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.Provider == provider)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetByUploadDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.UploadedAt >= startDate && f.UploadedAt <= endDate)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetRejectedFilesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.Status == FileStatus.Rejected)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.Status == FileStatus.Pending || f.Status == FileStatus.Uploaded)
            .OrderBy(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(f => f.Hash == hash, cancellationToken);
    }

    public async Task<bool> ExistsByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(f => f.StorageKey == storageKey, cancellationToken);
    }
}