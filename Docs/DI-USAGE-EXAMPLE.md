# Dependency Injection Usage Examples

This document shows how to configure and use the File Manager SDK in your application.

## Service Lifetime

**Important**: Storage providers are registered as **Singleton** for optimal performance and resource efficiency. See `SERVICE-LIFETIME-GUIDE.md` for detailed rationale.

## Basic Setup (ASP.NET Core)

```csharp
using FileManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register File Manager services
builder.Services.AddFileManager(builder.Configuration);
// Only the selected provider from configuration is registered!

var app = builder.Build();
app.Run();
```

**Important**: `AddFileManager` only registers the storage provider specified in `FileManagerOptions.DefaultProvider`. This means:
- ✅ Faster startup (only one provider initialized)
- ✅ Less memory usage
- ✅ Only need to configure the provider you're using
- ✅ Cleaner dependency graph

## Configuration (appsettings.json)

**If using MinIO (DefaultProvider = "MinIo"):**
```json
{
  "FileManager": {
    "DefaultProvider": "MinIo",
    "ValidationEnabled": true,
    "VirusScanningEnabled": false,
    "PresignedUrlExpiration": "01:00:00",
    "MaxFileSizeBytes": 5368709120
  },
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false
    }
    // No need to configure SeaweedFS or S3!
  }
}
```

**If using SeaweedFS (DefaultProvider = "SeaweedFs"):**
```json
{
  "FileManager": {
    "DefaultProvider": "SeaweedFs"
  },
  "Storage": {
    "SeaweedFS": {
      "FilerUrl": "http://localhost:8888",
      "BucketName": "files",
      "UseDirectUrls": false,
      "TimeoutSeconds": 30
    }
    // No need to configure MinIO or S3!
  }
}
```

**If using S3 (DefaultProvider = "S3"):**
```json
{
  "FileManager": {
    "DefaultProvider": "S3"
  },
  "Storage": {
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "AccessKey": "YOUR_ACCESS_KEY",
      "SecretKey": "YOUR_SECRET_KEY",
      "BucketName": "files",
      "Region": "us-east-1",
      "ForcePathStyle": false
    }
    // No need to configure MinIO or SeaweedFS!
  }
}
```

## Switching Storage Providers

To switch providers, simply change the `DefaultProvider` in configuration:

```json
{
  "FileManager": {
    "DefaultProvider": "SeaweedFs"  // or "MinIo" or "S3"
  }
}
```

## Usage Pattern 1: Default Storage Provider

Most common scenario - use the configured default provider:

```csharp
public class FileController : ControllerBase
{
    private readonly IObjectStorage _storage;
    private readonly ILogger<FileController> _logger;

    public FileController(
        IObjectStorage storage,
        ILogger<FileController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        using var stream = file.OpenReadStream();

        var request = new UploadRequest(
            Content: stream,
            Path: "uploads",
            FileName: file.FileName,
            ContentType: file.ContentType,
            Size: file.Length
        );

        var result = await _storage.UploadAsync(request);

        return Ok(new { result.StorageKey, result.ETag });
    }
}
```

## Usage Pattern 2: Multiple Providers (Advanced)

When you need to work with multiple storage providers simultaneously:

```csharp
using FileManager.Infrastructure;

// In Program.cs, add the factory
builder.Services.AddFileManager(builder.Configuration); // Registers default provider
builder.Services.AddStorageProviderFactory(builder.Configuration); // Registers ALL providers + factory

// WARNING: This registers ALL THREE providers (MinIO, SeaweedFS, S3)
// Only use this if you actually need multiple providers!

// When using factory, you must configure ALL providers in appsettings.json
{
  "FileManager": { "DefaultProvider": "MinIo" },
  "Storage": {
    "MinIO": { "Endpoint": "...", ... },
    "SeaweedFS": { "FilerUrl": "...", ... },
    "S3": { "ServiceUrl": "...", ... }
  }
}

// In your service
public class MultiStorageService
{
    private readonly IStorageProviderFactory _factory;

    public MultiStorageService(IStorageProviderFactory factory)
    {
        _factory = factory;
    }

    public async Task MigrateFiles()
    {
        // Get specific providers
        var minioStorage = _factory.GetStorage(StorageProvider.MinIo);
        var s3Storage = _factory.GetStorage(StorageProvider.S3);

        // Migrate from MinIO to S3
        var stream = await minioStorage.DownloadAsync("old-file.pdf");
        await s3Storage.UploadAsync(new UploadRequest(
            stream,
            "migrated",
            "old-file.pdf",
            "application/pdf",
            stream.Length
        ));
    }

    public async Task UseDefault()
    {
        // Or use the default provider from configuration
        var defaultStorage = _factory.GetDefaultStorage();
        await defaultStorage.HealthAsync();
    }
}
```

**When to Use Factory:**
- ✅ Migrating files between providers
- ✅ Multi-cloud backup strategy
- ✅ A/B testing different providers
- ❌ Normal single-provider usage (use default pattern instead)

## Usage Pattern 3: Accessing Configuration Options

```csharp
public class FileService
{
    private readonly FileManagerOptions _fileManagerOptions;
    private readonly MinIoOptions _minioOptions;

    public FileService(
        IOptions<FileManagerOptions> fileManagerOptions,
        IOptions<MinIoOptions> minioOptions)
    {
        _fileManagerOptions = fileManagerOptions.Value;
        _minioOptions = minioOptions.Value;
    }

    public async Task<bool> ValidateFileSize(long fileSize)
    {
        if (fileSize > _fileManagerOptions.MaxFileSizeBytes)
        {
            return false;
        }
        return true;
    }

    public TimeSpan GetUrlExpiration()
    {
        return _fileManagerOptions.PresignedUrlExpiration;
    }
}
```

## Console Application Setup

```csharp
using FileManager.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddFileManager(context.Configuration);
    })
    .Build();

var storage = host.Services.GetRequiredService<IObjectStorage>();
Console.WriteLine($"Using storage provider: {storage.Provider}");

await host.RunAsync();
```

## Environment-Specific Configuration

Override settings in `appsettings.Development.json`:

```json
{
  "FileManager": {
    "DefaultProvider": "MinIo",
    "ValidationEnabled": false,
    "VirusScanningEnabled": false
  },
  "Storage": {
    "MinIO": {
      "BucketName": "dev-files"
    }
  }
}
```

## Testing Configuration

For unit tests, you can provide test configuration:

```csharp
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string>
    {
        ["FileManager:DefaultProvider"] = "MinIo",
        ["Storage:MinIO:Endpoint"] = "localhost:9000",
        ["Storage:MinIO:AccessKey"] = "test",
        ["Storage:MinIO:SecretKey"] = "test",
        ["Storage:MinIO:BucketName"] = "test-bucket"
    })
    .Build();

var services = new ServiceCollection();
services.AddFileManager(configuration);

var serviceProvider = services.BuildServiceProvider();
var storage = serviceProvider.GetRequiredService<IObjectStorage>();
```
