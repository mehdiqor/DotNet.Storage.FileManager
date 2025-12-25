using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// SeaweedFS storage provider implementation.
/// </summary>
public class SeaweedFsService(
    IOptions<SeaweedFsOptions> options,
    ILogger<SeaweedFsService> logger
) : IObjectStorage
{
    public StorageProvider Provider => StorageProvider.SeaweedFs;

    public Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS upload will be implemented in Phase 3");
    }

    public Task<string> GetPresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS presigned upload URL will be implemented in Phase 3");
    }

    public Task<Stream> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS download will be implemented in Phase 3");
    }

    public Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS presigned download URL will be implemented in Phase 3");
    }

    public Task RemoveAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS remove will be implemented in Phase 3");
    }

    public Task RemoveBatchAsync(
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS batch remove will be implemented in Phase 3");
    }

    public Task<StorageObjectMetadata> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS get metadata will be implemented in Phase 3");
    }

    public Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS exists check will be implemented in Phase 3");
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SeaweedFS health check will be implemented in Phase 3");
    }
}