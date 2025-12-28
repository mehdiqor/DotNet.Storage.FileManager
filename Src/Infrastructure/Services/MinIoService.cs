using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Common.Utilities;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// MinIO storage provider implementation using Minio SDK.
/// Thread-safe singleton service.
/// </summary>
public class MinIoService : IObjectStorage
{
    private readonly MinIoOptions _options;
    private readonly ILogger<MinIoService> _logger;
    private readonly IMinioClient _minioClient;

    public StorageProvider Provider => StorageProvider.MinIo;

    public MinIoService(
        IOptions<MinIoOptions> options,
        ILogger<MinIoService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Initialize MinIO client
        var builder = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey);

        if (_options.UseSsl)
            builder = builder.WithSSL();

        if (!string.IsNullOrWhiteSpace(_options.Region))
            builder = builder.WithRegion(_options.Region);

        _minioClient = builder.Build();

        _logger.LogInformation(
            "MinIO client initialized for endpoint {Endpoint}, bucket {BucketName}, SSL: {UseSsl}",
            _options.Endpoint,
            _options.BucketName,
            _options.UseSsl);
    }

    public async Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);

            _logger.LogInformation(
                "Uploading file {FileName} to MinIO bucket {BucketName} with key {StorageKey}",
                request.FileName,
                _options.BucketName,
                storageKey);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Prepare metadata
            var metadata = request.Metadata ?? new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(request.Hash))
                metadata["x-amz-meta-hash"] = request.Hash;

            // Upload the file
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithStreamData(request.Content)
                .WithObjectSize(request.Size)
                .WithContentType(request.ContentType);

            if (metadata.Count > 0)
                putObjectArgs = putObjectArgs.WithHeaders(metadata);

            var response = await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded file {FileName} to MinIO with ETag {ETag}",
                request.FileName,
                response.Etag);

            return new UploadResult(
                StorageKey: storageKey,
                ETag: response.Etag,
                Size: request.Size,
                VersionId: null);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error uploading file {FileName}: {Message}",
                request.FileName,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error uploading file {FileName} to MinIO",
                request.FileName);
            throw;
        }
    }

    public async Task<string> GetPresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);

            _logger.LogInformation(
                "Generating presigned upload URL for {FileName} in bucket {BucketName}, expires in {ExpiresIn}",
                request.FileName,
                _options.BucketName,
                request.ExpiresIn);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            var presignedPutObjectArgs = new PresignedPutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithExpiry((int)request.ExpiresIn.TotalSeconds);

            var presignedUrl = await _minioClient.PresignedPutObjectAsync(presignedPutObjectArgs);

            _logger.LogInformation(
                "Generated presigned upload URL for {FileName}",
                request.FileName);

            return presignedUrl;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error generating presigned upload URL for {FileName}: {Message}",
                request.FileName,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating presigned upload URL for {FileName}",
                request.FileName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Downloading file {StorageKey} from MinIO bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithCallbackStream(stream => { stream.CopyTo(memoryStream); });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded file {StorageKey} from MinIO",
                storageKey);

            return memoryStream;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error downloading file {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error downloading file {StorageKey} from MinIO",
                storageKey);
            throw;
        }
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating presigned download URL for {StorageKey} in bucket {BucketName}, expires in {ExpiresIn}",
                storageKey,
                _options.BucketName,
                expiresIn);

            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithExpiry((int)expiresIn.TotalSeconds);

            var presignedUrl = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogInformation(
                "Generated presigned download URL for {StorageKey}",
                storageKey);

            return presignedUrl;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error generating presigned download URL for {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating presigned download URL for {StorageKey}",
                storageKey);
            throw;
        }
    }

    public async Task RemoveAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Removing file {StorageKey} from MinIO bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Successfully removed file {StorageKey} from MinIO",
                storageKey);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error removing file {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error removing file {StorageKey} from MinIO",
                storageKey);
            throw;
        }
    }

    public async Task RemoveBatchAsync(
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keyList = storageKeys.ToList();

            _logger.LogInformation(
                "Batch removing {Count} files from MinIO bucket {BucketName}",
                keyList.Count,
                _options.BucketName);

            var removeObjectsArgs = new RemoveObjectsArgs()
                .WithBucket(_options.BucketName)
                .WithObjects(keyList);

            var errors = await _minioClient.RemoveObjectsAsync(removeObjectsArgs, cancellationToken);

            if (errors.Count > 0)
            {
                _logger.LogWarning(
                    "Batch remove completed with {ErrorCount} errors out of {TotalCount} files",
                    errors.Count,
                    keyList.Count);

                foreach (var error in errors)
                {
                    _logger.LogError(
                        "Failed to remove {Key}: {Message}",
                        error.Key,
                        error.Message);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Successfully batch removed {Count} files from MinIO",
                    keyList.Count);
            }
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error during batch remove: {Message}",
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during batch remove from MinIO");
            throw;
        }
    }

    public async Task<StorageObjectMetadata> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting metadata for {StorageKey} from MinIO bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey);

            var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            var metadata = new Dictionary<string, string>();
            if (stat.MetaData != null)
            {
                foreach (var (key, value) in stat.MetaData)
                    if (value != null) metadata[key] = value;
            }

            _logger.LogInformation(
                "Successfully retrieved metadata for {StorageKey}",
                storageKey);

            return new StorageObjectMetadata(
                Key: storageKey,
                Size: stat.Size,
                ETag: stat.ETag,
                ContentType: stat.ContentType,
                LastModified: stat.LastModified,
                VersionId: stat.VersionId,
                Metadata: metadata.Count > 0 ? metadata : null);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error getting metadata for {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting metadata for {StorageKey} from MinIO",
                storageKey);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Checking if file {StorageKey} exists in MinIO bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            _logger.LogDebug("File {StorageKey} exists in MinIO", storageKey);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogDebug("File {StorageKey} does not exist in MinIO", storageKey);
            return false;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO error checking existence of {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error checking existence of {StorageKey} in MinIO",
                storageKey);
            throw;
        }
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Performing health check for MinIO bucket {BucketName}",
                _options.BucketName);

            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_options.BucketName);

            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            if (exists)
            {
                _logger.LogDebug("MinIO health check passed for bucket {BucketName}", _options.BucketName);
                return true;
            }

            _logger.LogWarning("MinIO health check failed: bucket {BucketName} does not exist", _options.BucketName);
            return false;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex,
                "MinIO health check failed: {Message}",
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during MinIO health check");
            return false;
        }
    }

    // ============================
    // Private Helper Methods
    // ============================

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(_options.BucketName);

        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
        if (exists) return;

        _logger.LogInformation(
            "Bucket {BucketName} does not exist, creating it",
            _options.BucketName);

        var makeBucketArgs = new MakeBucketArgs()
            .WithBucket(_options.BucketName);

        if (!string.IsNullOrWhiteSpace(_options.Region))
            makeBucketArgs = makeBucketArgs.WithLocation(_options.Region);

        await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

        _logger.LogInformation(
            "Successfully created bucket {BucketName}",
            _options.BucketName);
    }
}