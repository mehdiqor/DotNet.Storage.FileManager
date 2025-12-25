using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// MinIO storage provider implementation.
/// </summary>
public class MinIoService(
    IOptions<MinIoOptions> options,
    ILogger<MinIoService> logger
) : IObjectStorage
{
    public StorageProvider Provider => StorageProvider.MinIo;

    public Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO upload will be implemented in Phase 3");
    }

    public Task<string> GetPresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO presigned upload URL will be implemented in Phase 3");
    }

    public Task<Stream> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO download will be implemented in Phase 3");
    }

    public Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO presigned download URL will be implemented in Phase 3");
    }

    public Task RemoveAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO remove will be implemented in Phase 3");
    }

    public Task RemoveBatchAsync(
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO batch remove will be implemented in Phase 3");
    }

    public Task<StorageObjectMetadata> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO get metadata will be implemented in Phase 3");
    }

    public Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO exists check will be implemented in Phase 3");
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MinIO health check will be implemented in Phase 3");
    }
}