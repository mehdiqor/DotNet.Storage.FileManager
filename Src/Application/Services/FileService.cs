using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FileManager.Application.Services;

public class FileService(
    IObjectStorage objectStorage,
    ILogger<FileService> logger,
    IUnitOfWork unitOfWork
) : IFileService
{
    public Task<FileMetadata> UploadFileAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GeneratePresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> DownloadFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> DownloadFileByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        Guid fileId,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileMetadata?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileMetadata?> GetFileMetadataByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileMetadata?> GetFileMetadataByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<FileMetadata>> GetFilesByStatusAsync(
        FileStatus status,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFilesAsync(
        IEnumerable<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkAsValidatedAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkAsScannedAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RejectFileAsync(
        Guid fileId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistsAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistsByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}