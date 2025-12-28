# FileManager SDK Usage Guide

This guide provides comprehensive examples for common file management scenarios using the FileManager SDK.

## Table of Contents

- [Basic Operations](#basic-operations)
- [File Upload Scenarios](#file-upload-scenarios)
- [File Download Scenarios](#file-download-scenarios)
- [File Lifecycle Management](#file-lifecycle-management)
- [Querying Files](#querying-files)
- [Batch Operations](#batch-operations)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

## Basic Operations

### Service Injection

```csharp
public class FileManagerService
{
    private readonly IFileService _fileService;
    private readonly ILogger<FileManagerService> _logger;

    public FileManagerService(
        IFileService fileService,
        ILogger<FileManagerService> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }
}
```

## File Upload Scenarios

### Simple Upload

```csharp
public async Task<Guid> UploadSimpleFileAsync(IFormFile file)
{
    using var stream = file.OpenReadStream();

    var request = new UploadRequest(
        Content: stream,
        FileName: file.FileName,
        Path: "/uploads",
        Size: file.Length,
        ContentType: file.ContentType,
        Hash: null,
        Metadata: null
    );

    var fileMetadata = await _fileService.UploadFileAsync(request);

    _logger.LogInformation(
        "File uploaded: {FileId}, Status: {Status}",
        fileMetadata.Id,
        fileMetadata.Status);

    return fileMetadata.Id;
}
```

### Upload with Hash for Deduplication

```csharp
public async Task<Guid> UploadWithDeduplicationAsync(
    Stream fileStream,
    string fileName,
    string contentType)
{
    // Compute file hash
    var hash = await ComputeSHA256HashAsync(fileStream);
    fileStream.Position = 0; // Reset stream position

    // Check if file already exists
    if (await _fileService.FileExistsByHashAsync(hash))
    {
        _logger.LogInformation("File with hash {Hash} already exists", hash);
        var existing = await _fileService.GetFileMetadataByHashAsync(hash);
        return existing!.Id;
    }

    var request = new UploadRequest(
        Content: fileStream,
        FileName: fileName,
        Path: "/documents",
        Size: fileStream.Length,
        ContentType: contentType,
        Hash: hash,
        Metadata: null
    );

    try
    {
        var fileMetadata = await _fileService.UploadFileAsync(request);
        return fileMetadata.Id;
    }
    catch (DuplicateFileException ex)
    {
        _logger.LogWarning("Duplicate file detected: {Hash}", ex.Hash);
        throw;
    }
}

private async Task<string> ComputeSHA256HashAsync(Stream stream)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashBytes = await sha256.ComputeHashAsync(stream);
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
}
```

### Upload with Custom Metadata

```csharp
public async Task<Guid> UploadWithMetadataAsync(
    IFormFile file,
    string author,
    string department)
{
    using var stream = file.OpenReadStream();

    var metadata = new Dictionary<string, string>
    {
        ["author"] = author,
        ["department"] = department,
        ["uploadedBy"] = "system",
        ["version"] = "1.0"
    };

    var request = new UploadRequest(
        Content: stream,
        FileName: file.FileName,
        Path: $"/departments/{department}",
        Size: file.Length,
        ContentType: file.ContentType,
        Hash: null,
        Metadata: metadata
    );

    var fileMetadata = await _fileService.UploadFileAsync(request);
    return fileMetadata.Id;
}
```

### Presigned URL Upload (Direct Client Upload)

```csharp
public async Task<PresignedUploadResponse> GenerateUploadUrlAsync(
    string fileName,
    string contentType,
    long maxSize)
{
    var request = new PresignedUploadRequest(
        Path: "/client-uploads",
        FileName: fileName,
        ContentType: contentType,
        ExpiresIn: TimeSpan.FromHours(1),
        MaxSize: maxSize
    );

    var response = await _fileService.GeneratePresignedUploadUrlAsync(request);

    _logger.LogInformation(
        "Presigned upload URL generated for {FileName}, FileId: {FileId}",
        fileName,
        response.FileId);

    return response;
}

// Client-side usage:
// 1. Get presigned URL from API
// 2. Upload file directly to storage using URL
// 3. Optionally notify API when upload completes
```

## File Download Scenarios

### Download by File ID

```csharp
public async Task<FileStreamResult> DownloadFileAsync(Guid fileId)
{
    try
    {
        var stream = await _fileService.DownloadFileAsync(fileId);
        var metadata = await _fileService.GetFileMetadataAsync(fileId);

        if (metadata == null)
            throw new FileNotFoundException(fileId);

        return new FileStreamResult(stream, metadata.ContentType)
        {
            FileDownloadName = metadata.FileName
        };
    }
    catch (FileNotAvailableException ex)
    {
        _logger.LogWarning(
            "File not available: {FileId}, Status: {Status}",
            ex.FileId,
            ex.CurrentStatus);
        throw;
    }
}
```

### Generate Presigned Download URL

```csharp
public async Task<string> GetDownloadUrlAsync(
    Guid fileId,
    TimeSpan? expiresIn = null)
{
    var expiration = expiresIn ?? TimeSpan.FromMinutes(15);

    var url = await _fileService.GeneratePresignedDownloadUrlAsync(
        fileId,
        expiration);

    _logger.LogInformation(
        "Download URL generated for {FileId}, expires in {Minutes} minutes",
        fileId,
        expiration.TotalMinutes);

    return url;
}
```

### Download by Storage Key

```csharp
public async Task<Stream> DownloadByKeyAsync(string storageKey)
{
    // Direct download without metadata check
    var stream = await _fileService.DownloadFileByKeyAsync(storageKey);

    _logger.LogInformation("File downloaded by key: {StorageKey}", storageKey);

    return stream;
}
```

## File Lifecycle Management

### Validation Workflow (Webhook-based)

The SDK handles file validation automatically when object storage webhooks fire after upload:

```csharp
// Webhook handler receives upload notification from object storage
[HttpPost("webhooks/file-uploaded")]
public async Task<IActionResult> HandleFileUploadedWebhook([FromBody] S3EventNotification notification)
{
    foreach (var record in notification.Records)
    {
        var storageKey = record.S3.Object.Key;

        // Create actual metadata from webhook
        var actualMetadata = new StorageObjectMetadata(
            Key: storageKey,
            Size: record.S3.Object.Size,
            ETag: record.S3.Object.ETag,
            ContentType: null, // S3 doesn't include ContentType - SDK fetches it automatically
            LastModified: record.EventTime
        );

        // ValidateFileAsync automatically:
        // 1. Fetches complete metadata if ContentType is null
        // 2. Compares actual metadata with database expectations
        // 3. Validates file size within limits
        // 4. If validation passes & virus scanning disabled → status = Available
        // 5. If validation passes & virus scanning enabled → status = Uploaded
        // 6. If validation fails → deletes from storage, status = Rejected
        await _fileService.ValidateFileAsync(storageKey, actualMetadata);

        _logger.LogInformation("File validated: {StorageKey}", storageKey);
    }

    return Ok();
}
```

### Checking File Status

```csharp
public async Task<FileStatus> CheckFileStatusAsync(Guid fileId)
{
    var file = await _fileService.GetFileMetadataAsync(fileId);

    if (file == null)
        throw new FileNotFoundException(fileId);

    _logger.LogInformation(
        "File: {FileId}, Status: {Status}",
        fileId,
        file.Status);

    return file.Status;
}
```

### Getting Pending Files

```csharp
// Get all files awaiting validation or scanning
public async Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync()
{
    var pendingFiles = await _fileService.GetPendingFilesAsync();

    _logger.LogInformation(
        "Found {Count} pending files",
        pendingFiles.Count);

    return pendingFiles;
}
```

## Querying Files

### Get File by ID

```csharp
public async Task<FileMetadata?> GetFileDetailsAsync(Guid fileId)
{
    var file = await _fileService.GetFileMetadataAsync(fileId);

    if (file != null)
    {
        _logger.LogInformation(
            "File found: {FileName}, Status: {Status}, Size: {Size} bytes",
            file.FileName,
            file.Status,
            file.Size);
    }

    return file;
}
```

### Get Files by Status

```csharp
public async Task<List<FileMetadata>> GetFilesByStatusAsync(
    FileStatus status)
{
    var files = await _fileService.GetFilesByStatusAsync(status);

    _logger.LogInformation(
        "Found {Count} files with status: {Status}",
        files.Count,
        status);

    return files.ToList();
}
```

### Get Pending Files for Processing

```csharp
public async Task ProcessPendingFilesAsync()
{
    var pendingFiles = await _fileService.GetPendingFilesAsync();

    _logger.LogInformation(
        "Processing {Count} pending files",
        pendingFiles.Count);

    foreach (var file in pendingFiles)
    {
        try
        {
            await ProcessUploadedFileAsync(file.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process file: {FileId}",
                file.Id);
        }
    }
}
```

### Check File Existence

```csharp
public async Task<bool> CheckFileExistsAsync(Guid fileId)
{
    var exists = await _fileService.FileExistsAsync(fileId);

    _logger.LogInformation(
        "File {FileId} exists: {Exists}",
        fileId,
        exists);

    return exists;
}

public async Task<bool> CheckDuplicateAsync(string hash)
{
    var exists = await _fileService.FileExistsByHashAsync(hash);

    if (exists)
    {
        var file = await _fileService.GetFileMetadataByHashAsync(hash);
        _logger.LogInformation(
            "Duplicate found: FileId: {FileId}",
            file?.Id);
    }

    return exists;
}
```

## Batch Operations

### Delete Multiple Files

```csharp
public async Task DeleteMultipleFilesAsync(IEnumerable<Guid> fileIds)
{
    var ids = fileIds.ToList();

    _logger.LogInformation(
        "Deleting {Count} files",
        ids.Count);

    await _fileService.DeleteFilesAsync(ids);

    _logger.LogInformation("Files deleted successfully");
}
```

### Bulk File Processing

```csharp
public async Task ProcessFilesBatchAsync(
    List<Guid> fileIds,
    Func<Guid, Task> processFunc)
{
    var tasks = fileIds.Select(async fileId =>
    {
        try
        {
            await processFunc(fileId);
            _logger.LogInformation("Processed: {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process: {FileId}",
                fileId);
        }
    });

    await Task.WhenAll(tasks);
}
```

## Error Handling

### Comprehensive Error Handling

```csharp
public async Task<Result> UploadWithErrorHandlingAsync(IFormFile file)
{
    try
    {
        using var stream = file.OpenReadStream();

        var request = new UploadRequest(
            Content: stream,
            FileName: file.FileName,
            Path: "/uploads",
            Size: file.Length,
            ContentType: file.ContentType,
            Hash: null,
            Metadata: null
        );

        var fileMetadata = await _fileService.UploadFileAsync(request);

        return Result.Success(fileMetadata.Id);
    }
    catch (DuplicateFileException ex)
    {
        _logger.LogWarning("Duplicate file: {Hash}", ex.Hash);
        return Result.Failure("File already exists");
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning("Validation failed: {Errors}", ex.Errors);
        return Result.Failure("Invalid file");
    }
    catch (StorageProviderException ex)
    {
        _logger.LogError(ex, "Storage error");
        return Result.Failure("Storage service unavailable");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during upload");
        return Result.Failure("Upload failed");
    }
}

public class Result
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? FileId { get; init; }

    public static Result Success(Guid fileId) => new()
    {
        IsSuccess = true,
        FileId = fileId
    };

    public static Result Failure(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
```

## Best Practices

### 1. Always Dispose Streams

```csharp
// GOOD
using var stream = File.OpenRead("file.txt");
await _fileService.UploadFileAsync(request);

// BAD
var stream = File.OpenRead("file.txt");
await _fileService.UploadFileAsync(request);
// Stream not disposed!
```

### 2. Use Hash for Deduplication

```csharp
// Compute hash before upload to detect duplicates early
var hash = await ComputeHashAsync(stream);
if (await _fileService.FileExistsByHashAsync(hash))
{
    // File already exists, skip upload
    return existingFileId;
}
```

### 3. Handle FileNotAvailableException

```csharp
try
{
    var stream = await _fileService.DownloadFileAsync(fileId);
}
catch (FileNotAvailableException ex)
{
    // File exists but not ready (still being validated/scanned)
    return StatusCode(425, $"File not ready: {ex.CurrentStatus}");
}
```

### 4. Use Presigned URLs for Large Files

```csharp
// For files > 10MB, use presigned URLs for direct client upload
if (fileSize > 10 * 1024 * 1024)
{
    var response = await _fileService.GeneratePresignedUploadUrlAsync(request);
    return Ok(new { uploadUrl = response.UploadUrl, fileId = response.FileId });
}
```

### 5. Implement Retry Logic

```csharp
public async Task<FileMetadata> UploadWithRetryAsync(UploadRequest request)
{
    var policy = Policy
        .Handle<StorageProviderException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, timeSpan, attempt, context) =>
            {
                _logger.LogWarning(
                    "Upload attempt {Attempt} failed, retrying in {Delay}s",
                    attempt,
                    timeSpan.TotalSeconds);
            });

    return await policy.ExecuteAsync(async () =>
        await _fileService.UploadFileAsync(request));
}
```

### 6. Process Pending Files in Background

```csharp
public class FileProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileProcessingBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

            try
            {
                var pendingFiles = await fileService.GetPendingFilesAsync(stoppingToken);

                foreach (var file in pendingFiles)
                {
                    await ProcessFileAsync(fileService, file.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending files");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Next Steps

- [Configuration Reference](CONFIGURATION.md)
- [Database Configuration](DATABASE-CONFIGURATION.md)
- [API Reference](API-REFERENCE.md)
- [Performance Tuning](PERFORMANCE.md)
