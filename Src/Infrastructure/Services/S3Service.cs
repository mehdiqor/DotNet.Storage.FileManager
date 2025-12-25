using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// S3-compatible storage provider implementation.
/// </summary>
public class S3Service(
    IOptions<S3Options> options,
    ILogger<S3Service> logger
) : IObjectStorage
{
    public StorageProvider Provider => StorageProvider.S3;

    public Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 upload will be implemented in Phase 3");
    }

    public Task<string> GetPresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 presigned upload URL will be implemented in Phase 3");
    }

    public Task<Stream> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 download will be implemented in Phase 3");
    }

    public Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 presigned download URL will be implemented in Phase 3");
    }

    public Task RemoveAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 remove will be implemented in Phase 3");
    }

    public Task RemoveBatchAsync(
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 batch remove will be implemented in Phase 3");
    }

    public Task<StorageObjectMetadata> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 get metadata will be implemented in Phase 3");
    }

    public Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 exists check will be implemented in Phase 3");
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("S3 health check will be implemented in Phase 3");
    }
}