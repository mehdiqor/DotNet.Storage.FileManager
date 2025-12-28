using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Common.Utilities;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wangkanai.Extensions;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// S3-compatible storage provider implementation using AWS SDK.
/// Thread-safe singleton service.
/// </summary>
public class S3Service : IObjectStorage
{
    private readonly S3Options _options;
    private readonly ILogger<S3Service> _logger;
    private readonly IAmazonS3 _s3Client;

    public StorageProvider Provider => StorageProvider.S3;

    public S3Service(
        IOptions<S3Options> options,
        ILogger<S3Service> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Initialize AWS S3 client
        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = _options.ForcePathStyle,
            RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
        };

        _s3Client = new AmazonS3Client(credentials, config);

        _logger.LogInformation(
            "S3 client initialized for endpoint {ServiceUrl}, bucket {BucketName}, region {Region}",
            _options.ServiceUrl,
            _options.BucketName,
            _options.Region);
    }

    public async Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);

            _logger.LogInformation(
                "Uploading file {FileName} to S3 bucket {BucketName} with key {StorageKey}",
                request.FileName,
                _options.BucketName,
                storageKey);

            // Ensure bucket exists
            await EnsureBucketExistsAsync();

            // Prepare upload request
            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey,
                InputStream = request.Content,
                ContentType = request.ContentType
            };

            // Add metadata
            if (request.Metadata != null)
            {
                foreach (var (key, value) in request.Metadata)
                    putRequest.Metadata.Add($"x-amz-meta-{key}", value);
            }

            if (!string.IsNullOrWhiteSpace(request.Hash))
                putRequest.Metadata.Add("x-amz-meta-hash", request.Hash);

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded file {FileName} to S3 with ETag {ETag}",
                request.FileName,
                response.ETag);

            return new UploadResult(
                StorageKey: storageKey,
                ETag: response.ETag,
                Size: request.Size,
                VersionId: response.VersionId);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error uploading file {FileName}: {ErrorCode} - {Message}",
                request.FileName,
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error uploading file {FileName} to S3",
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
            await EnsureBucketExistsAsync();

            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.Add(request.ExpiresIn),
                ContentType = request.ContentType
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

            _logger.LogInformation(
                "Generated presigned upload URL for {FileName}",
                request.FileName);

            return presignedUrl;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error generating presigned upload URL for {FileName}: {ErrorCode} - {Message}",
                request.FileName,
                ex.ErrorCode,
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
                "Downloading file {StorageKey} from S3 bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var getRequest = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded file {StorageKey} from S3",
                storageKey);

            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning("File {StorageKey} not found in S3", storageKey);
            throw new FileNotFoundException($"File not found: {storageKey}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error downloading file {StorageKey}: {ErrorCode} - {Message}",
                storageKey,
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error downloading file {StorageKey} from S3",
                storageKey);
            throw;
        }
    }

    public Task<string> GetPresignedDownloadUrlAsync(
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

            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiresIn)
            };

            var presignedUrl = _s3Client.GetPreSignedURL(presignedRequest);

            _logger.LogInformation(
                "Generated presigned download URL for {StorageKey}",
                storageKey);

            return Task.FromResult(presignedUrl);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error generating presigned download URL for {StorageKey}: {ErrorCode} - {Message}",
                storageKey,
                ex.ErrorCode,
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
                "Removing file {StorageKey} from S3 bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation(
                "Successfully removed file {StorageKey} from S3",
                storageKey);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error removing file {StorageKey}: {ErrorCode} - {Message}",
                storageKey,
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error removing file {StorageKey} from S3",
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
                "Batch removing {Count} files from S3 bucket {BucketName}",
                keyList.Count,
                _options.BucketName);

            // S3 supports batch deletion of up to 1000 objects
            var batches = keyList.Chunk(1000);

            foreach (var batch in batches)
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = batch.Select(key => new KeyVersion { Key = key }).ToList()
                };

                var response = await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);
                if (response.DeleteErrors.IsEmpty()) continue;

                _logger.LogWarning(
                    "Batch remove completed with {ErrorCount} errors in current batch",
                    response.DeleteErrors.Count);

                foreach (var error in response.DeleteErrors)
                {
                    _logger.LogError(
                        "Failed to remove {Key}: {Code} - {Message}",
                        error.Key,
                        error.Code,
                        error.Message);
                }
            }

            _logger.LogInformation(
                "Successfully batch removed {Count} files from S3",
                keyList.Count);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error during batch remove: {ErrorCode} - {Message}",
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch remove from S3");
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
                "Getting metadata for {StorageKey} from S3 bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(metadataRequest, cancellationToken);

            var metadata = new Dictionary<string, string>();
            if (response.Metadata != null)
            {
                foreach (var key in response.Metadata.Keys)
                {
                    var value = response.Metadata[key];
                    if (value != null) metadata[key] = value;
                }
            }

            _logger.LogInformation(
                "Successfully retrieved metadata for {StorageKey}",
                storageKey);

            return new StorageObjectMetadata(
                Key: storageKey,
                Size: response.ContentLength,
                ETag: response.ETag,
                ContentType: response.Headers.ContentType ?? "application/octet-stream",
                LastModified: response.LastModified ?? DateTime.UtcNow,
                VersionId: response.VersionId,
                Metadata: metadata.Count > 0 ? metadata : null);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "NoSuchKey" or "NotFound")
        {
            _logger.LogWarning("File {StorageKey} not found in S3", storageKey);
            throw new FileNotFoundException($"File not found: {storageKey}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error getting metadata for {StorageKey}: {ErrorCode} - {Message}",
                storageKey,
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting metadata for {StorageKey} from S3",
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
                "Checking if file {StorageKey} exists in S3 bucket {BucketName}",
                storageKey,
                _options.BucketName);

            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            await _s3Client.GetObjectMetadataAsync(metadataRequest, cancellationToken);

            _logger.LogDebug("File {StorageKey} exists in S3", storageKey);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "NoSuchKey" or "NotFound")
        {
            _logger.LogDebug("File {StorageKey} does not exist in S3", storageKey);
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 error checking existence of {StorageKey}: {ErrorCode} - {Message}",
                storageKey,
                ex.ErrorCode,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error checking existence of {StorageKey} in S3",
                storageKey);
            throw;
        }
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Performing health check for S3 bucket {BucketName}",
                _options.BucketName);

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                MaxKeys = 1
            };

            await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);

            _logger.LogDebug("S3 health check passed for bucket {BucketName}", _options.BucketName);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "S3 health check failed: {ErrorCode} - {Message}",
                ex.ErrorCode,
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during S3 health check");
            return false;
        }
    }

    // ============================
    // Private Helper Methods
    // ============================

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            await _s3Client.EnsureBucketExistsAsync(_options.BucketName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not ensure bucket {BucketName} exists: {ErrorCode} - {Message}",
                _options.BucketName,
                ex.ErrorCode,
                ex.Message);
        }
    }
}