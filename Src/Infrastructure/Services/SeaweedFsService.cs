using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Common.Utilities;
using FileManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// SeaweedFS storage provider implementation using HTTP API.
/// Thread-safe singleton service.
/// </summary>
public class SeaweedFsService : IObjectStorage
{
    private readonly SeaweedFsOptions _options;
    private readonly ILogger<SeaweedFsService> _logger;
    private readonly HttpClient _httpClient;

    public StorageProvider Provider => StorageProvider.SeaweedFs;

    public SeaweedFsService(
        IOptions<SeaweedFsOptions> options,
        ILogger<SeaweedFsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("SeaweedFS");
        _httpClient.BaseAddress = new Uri(_options.FilerUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _logger.LogInformation(
            "SeaweedFS client initialized for Filer {FilerUrl}, bucket {BucketName}",
            _options.FilerUrl,
            _options.BucketName);
    }

    public async Task<UploadResult> UploadAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Uploading file {FileName} to SeaweedFS with key {StorageKey}",
                request.FileName,
                storageKey);

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(request.Content);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
            streamContent.Headers.ContentLength = request.Size;

            content.Add(streamContent, "file", request.FileName);

            // Add custom metadata as headers
            if (request.Metadata != null)
            {
                foreach (var (key, value) in request.Metadata)
                    content.Headers.Add($"X-SeaweedFS-{key}", value);
            }

            if (!string.IsNullOrWhiteSpace(request.Hash))
                content.Headers.Add("X-SeaweedFS-Hash", request.Hash);

            var response = await _httpClient.PostAsync(fileUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var eTag = root.TryGetProperty("eTag", out var eTagProp) ? eTagProp.GetString() : string.Empty;
            var size = root.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : request.Size;

            _logger.LogInformation(
                "Successfully uploaded file {FileName} to SeaweedFS with ETag {ETag}",
                request.FileName,
                eTag);

            return new UploadResult(
                StorageKey: storageKey,
                ETag: eTag ?? string.Empty,
                Size: size);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS HTTP error uploading file {FileName}: {Message}",
                request.FileName,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error uploading file {FileName} to SeaweedFS",
                request.FileName);
            throw;
        }
    }

    public Task<string> GetPresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Generating presigned upload URL for {FileName}",
                request.FileName);

            // SeaweedFS supports direct URLs or HMAC-signed URLs
            var presignedUrl = _options.UseDirectUrls || string.IsNullOrWhiteSpace(_options.SecretKey)
                ? fileUrl
                : GenerateSignedUrl(fileUrl, "POST", request.ExpiresIn);

            _logger.LogInformation(
                "Generated presigned upload URL for {FileName}",
                request.FileName);

            return Task.FromResult(presignedUrl);
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
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Downloading file {StorageKey} from SeaweedFS",
                storageKey);

            var response = await _httpClient.GetAsync(fileUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {StorageKey} not found in SeaweedFS", storageKey);
                throw new FileNotFoundException($"File not found: {storageKey}");
            }

            response.EnsureSuccessStatusCode();

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded file {StorageKey} from SeaweedFS",
                storageKey);

            return memoryStream;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS HTTP error downloading file {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error downloading file {StorageKey} from SeaweedFS",
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
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Generating presigned download URL for {StorageKey}, expires in {ExpiresIn}",
                storageKey,
                expiresIn);

            // SeaweedFS supports direct URLs or HMAC-signed URLs
            var presignedUrl = _options.UseDirectUrls || string.IsNullOrWhiteSpace(_options.SecretKey)
                ? fileUrl
                : GenerateSignedUrl(fileUrl, "GET", expiresIn);

            _logger.LogInformation(
                "Generated presigned download URL for {StorageKey}",
                storageKey);

            return Task.FromResult(presignedUrl);
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
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Removing file {StorageKey} from SeaweedFS",
                storageKey);

            var response = await _httpClient.DeleteAsync(fileUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {StorageKey} not found in SeaweedFS, nothing to delete", storageKey);
                return;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Successfully removed file {StorageKey} from SeaweedFS",
                storageKey);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS HTTP error removing file {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error removing file {StorageKey} from SeaweedFS",
                storageKey);
            throw;
        }
    }

    public async Task RemoveBatchAsync(
        IEnumerable<string> storageKeys,
        CancellationToken cancellationToken = default)
    {
        var keyList = storageKeys.ToList();

        _logger.LogInformation(
            "Batch removing {Count} files from SeaweedFS",
            keyList.Count);

        var errors = new List<string>();

        foreach (var key in keyList)
        {
            try
            {
                await RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove {Key} during batch operation", key);
                errors.Add(key);
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Batch remove completed with {ErrorCount} errors out of {TotalCount} files",
                errors.Count,
                keyList.Count);
        }
        else
        {
            _logger.LogInformation(
                "Successfully batch removed {Count} files from SeaweedFS",
                keyList.Count);
        }
    }

    public async Task<StorageObjectMetadata> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogInformation(
                "Getting metadata for {StorageKey} from SeaweedFS",
                storageKey);

            using var request = new HttpRequestMessage(HttpMethod.Head, fileUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {StorageKey} not found in SeaweedFS", storageKey);
                throw new FileNotFoundException($"File not found: {storageKey}");
            }

            response.EnsureSuccessStatusCode();

            var size = response.Content.Headers.ContentLength ?? 0;
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var lastModified = response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.UtcNow;
            var eTag = response.Headers.ETag?.Tag ?? string.Empty;

            // Extract custom metadata from headers
            var metadata = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                if (header.Key.StartsWith("X-SeaweedFS-", StringComparison.OrdinalIgnoreCase))
                {
                    var key = header.Key.Substring(12);
                    var value = string.Join(", ", header.Value);
                    metadata[key] = value;
                }
            }

            _logger.LogInformation(
                "Successfully retrieved metadata for {StorageKey}",
                storageKey);

            return new StorageObjectMetadata(
                Key: storageKey,
                Size: size,
                ETag: eTag,
                ContentType: contentType,
                LastModified: lastModified,
                Metadata: metadata.Count > 0 ? metadata : null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS HTTP error getting metadata for {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting metadata for {StorageKey} from SeaweedFS",
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
            var fileUrl = BuildFileUrl(storageKey);

            _logger.LogDebug(
                "Checking if file {StorageKey} exists in SeaweedFS",
                storageKey);

            using var request = new HttpRequestMessage(HttpMethod.Head, fileUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            var exists = response.StatusCode == HttpStatusCode.OK;

            _logger.LogDebug(
                "File {StorageKey} {ExistsStatus} in SeaweedFS",
                storageKey,
                exists ? "exists" : "does not exist");

            return exists;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS HTTP error checking existence of {StorageKey}: {Message}",
                storageKey,
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error checking existence of {StorageKey} in SeaweedFS",
                storageKey);
            return false;
        }
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Performing health check for SeaweedFS at {FilerUrl}",
                _options.FilerUrl);

            // Check if filer is reachable
            var response = await _httpClient.GetAsync("/", cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;

            if (isHealthy)
            {
                _logger.LogDebug("SeaweedFS health check passed for {FilerUrl}", _options.FilerUrl);
                return true;
            }

            _logger.LogWarning(
                "SeaweedFS health check failed for {FilerUrl}: Status code {StatusCode}",
                _options.FilerUrl,
                response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "SeaweedFS health check failed: {Message}",
                ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during SeaweedFS health check");
            return false;
        }
    }

    // ============================
    // Private Helper Methods
    // ============================

    private string BuildFileUrl(string storageKey)
    {
        var cleanKey = storageKey.TrimStart('/');
        return $"/{_options.BucketName}/{cleanKey}";
    }

    private string GenerateSignedUrl(string url, string method, TimeSpan expiresIn)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return url;

        var expiration = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds();
        var message = $"{method}\n{url}\n{expiration}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var signature = Convert.ToBase64String(hash);

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}signature={Uri.EscapeDataString(signature)}&expires={expiration}";
    }
}