# FileManager SDK

A comprehensive, provider-agnostic file management SDK for .NET 9 that simplifies file storage operations across multiple cloud and on-premise object storage providers.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

## Features

- 🔌 **Provider-Agnostic Interface**: Work with MinIO, SeaweedFS, and S3-compatible storage through a unified API
- 🗄️ **Multi-Database Support**: SQL Server, PostgreSQL, MySQL, and SQLite
- ✅ **Built-in Validation**: FluentValidation-powered request validation with configurable rules
- 🔒 **Security First**: Hash-based deduplication, built-in ClamAV virus scanning, and status workflow
- 🦠 **Virus Scanning**: Integrated ClamAV support with automatic scanning, caching, and extensible scanner interface
- 📊 **File Lifecycle Management**: Automatic status transitions (Pending → Validated → Scanned → Available)
- 🎯 **Clean Architecture**: DDD principles with Domain, Application, and Infrastructure layers
- 🔄 **Event-Driven Architecture**: Domain events with pluggable message broker support (RabbitMQ, Azure Service Bus, AWS SQS, Kafka, Redis)
- 🚀 **Performance Optimized**: Batch operations, connection pooling, efficient queries, and scan result caching
- 📦 **Easy Integration**: Simple dependency injection with minimal configuration
- 🔌 **Extensible**: User-implemented message brokers and virus scanning services

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Supported Providers](#supported-providers)
- [Contributing](#contributing)
- [License](#license)

## Installation

```bash
dotnet add package FileManager.SDK
```

Or add to your `.csproj`:

```xml
<PackageReference Include="FileManager.SDK" Version="1.0.0" />
```

## Quick Start

### 1. Configure Services

```csharp
using FileManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register FileManager with your preferred storage provider
builder.Services.AddFileManager(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 2. Configure appsettings.json

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
  },
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;"
  },
  "ClamAV": {
    "Server": "localhost",
    "Port": 3310,
    "MaxFileSizeBytes": 104857600,
    "ScanTimeoutSeconds": 60,
    "ConnectionTimeoutSeconds": 10,
    "MaxRetries": 3,
    "EnableCaching": true,
    "CacheExpirationMinutes": 60
  }
}
```

### 3. Use the SDK

```csharp
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        using var stream = file.OpenReadStream();

        var request = new UploadRequest(
            Content: stream,
            FileName: file.FileName,
            Path: "/uploads",
            Size: file.Length,
            ContentType: file.ContentType,
            Hash: null, // Optional: provide file hash for deduplication
            Metadata: null
        );

        var fileMetadata = await _fileService.UploadFileAsync(request);

        return Ok(new { fileMetadata.Id, fileMetadata.Status });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var stream = await _fileService.DownloadFileAsync(id);
        var metadata = await _fileService.GetFileMetadataAsync(id);

        return File(stream, metadata.ContentType, metadata.FileName);
    }
}
```

## Configuration

### Storage Providers

Choose one of the supported storage providers:

**MinIO** (S3-compatible, self-hosted):
```json
{
  "FileManager": { "DefaultProvider": "MinIo" },
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false
    }
  }
}
```

**SeaweedFS** (Distributed file system):
```json
{
  "FileManager": { "DefaultProvider": "SeaweedFs" },
  "Storage": {
    "SeaweedFS": {
      "FilerUrl": "http://localhost:8888",
      "BucketName": "files"
    }
  }
}
```

**AWS S3** (or S3-compatible):
```json
{
  "FileManager": { "DefaultProvider": "S3" },
  "Storage": {
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "BucketName": "your-bucket",
      "Region": "us-east-1"
    }
  }
}
```

### Database Configuration

See [Database Configuration Guide](Docs/DATABASE-CONFIGURATION.md) for detailed setup instructions.

### Event Handling Configuration

FileManager SDK supports event-driven architecture through a **user-implemented** event handler pattern. The SDK provides the `IEventPublisher` interface, and you implement it to handle file lifecycle events however you want.

**What You Can Do**:
- Publish events to message brokers (RabbitMQ, Azure Service Bus, AWS SQS, Kafka, Redis)
- Trigger file validation workflows
- Send email/SMS notifications
- Update analytics or audit logs
- Execute custom business logic

**Quick Example** (Publish to RabbitMQ):
```csharp
// Implement IEventPublisher to publish events to RabbitMQ
public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var routingKey = $"file.{domainEvent.GetType().Name.ToLowerInvariant()}";
            var json = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "filemanager.events",
                routingKey: routingKey,
                body: body);
        }

        await Task.CompletedTask;
    }
}

// Register in DI
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
```

**See the complete implementation guide**: [Event Handling Guide](Docs/EVENT-HANDLING-GUIDE.md)

This guide includes:
- Complete implementation examples for RabbitMQ, Azure Service Bus, AWS SQS, and more
- Validation workflow patterns
- Best practices for error handling, retry logic, and idempotency
- Email notification examples
- Troubleshooting guide

### Virus Scanning Configuration

FileManager SDK includes built-in ClamAV integration for virus scanning. When enabled, files are automatically scanned after validation.

**Enable Virus Scanning**:
```json
{
  "FileManager": {
    "VirusScanningEnabled": true
  },
  "ClamAV": {
    "Server": "localhost",
    "Port": 3310,
    "MaxFileSizeBytes": 104857600,
    "ScanTimeoutSeconds": 60,
    "EnableCaching": true
  }
}
```

**Docker Setup** (included in docker-compose.yml):
```bash
# Start ClamAV along with other services
docker-compose up -d clamav

# Check ClamAV status (may take 2-3 minutes to load virus definitions)
docker logs clamav
```

**How It Works**:
1. File is uploaded → status: `Pending`
2. Validation passes → status: `Uploaded`
3. Virus scan runs automatically → status: `Available` (clean) or `Rejected` (infected)

**Configuration Options**:
- `Server`: ClamAV server hostname (default: `localhost`)
- `Port`: ClamAV daemon port (default: `3310`)
- `MaxFileSizeBytes`: Max file size to scan (default: 100 MB)
- `ScanTimeoutSeconds`: Scan operation timeout (default: 60s)
- `EnableCaching`: Cache scan results by file hash (default: `true`)
- `CacheExpirationMinutes`: Cache expiration time (default: 60 min)

**Custom Virus Scanner**:
You can implement your own virus scanner by implementing `IVirusScanningService`:
```csharp
public class CustomVirusScanner : IVirusScanningService
{
    public async Task<ScanResult> ScanAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Your scanning logic (e.g., VirusTotal API, Windows Defender, etc.)
        return new ScanResult(IsClean: true);
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        // Check if your scanner service is available
        return true;
    }
}

// Register your custom scanner
builder.Services.AddSingleton<IVirusScanningService, CustomVirusScanner>();
```

**Health Check**:
```csharp
// Check if ClamAV is available before scanning
var isHealthy = await _virusScanningService.HealthAsync();
if (!isHealthy)
{
    _logger.LogWarning("ClamAV is not available");
    // Handle unavailability (skip scan, queue for later, etc.)
}
```

### Advanced Configuration

For multi-provider scenarios, dependency injection patterns, and advanced features:
- [Dependency Injection Usage](Docs/DI-USAGE-EXAMPLE.md)
- [Service Lifetime Guide](Docs/SERVICE-LIFETIME-GUIDE.md)
- [Configuration Reference](Docs/CONFIGURATION.md)

## Usage Examples

### File Upload with Validation

```csharp
var request = new UploadRequest(
    Content: fileStream,
    FileName: "document.pdf",
    Path: "/documents",
    Size: fileStream.Length,
    ContentType: "application/pdf",
    Hash: ComputeSHA256(fileStream), // Optional deduplication
    Metadata: new Dictionary<string, string>
    {
        ["author"] = "John Doe",
        ["department"] = "Engineering"
    }
);

try
{
    var file = await _fileService.UploadFileAsync(request);
    Console.WriteLine($"File uploaded: {file.Id}, Status: {file.Status}");
}
catch (DuplicateFileException ex)
{
    Console.WriteLine($"File already exists: {ex.Hash}");
}
```

### Presigned URLs for Direct Upload

```csharp
var request = new PresignedUploadRequest(
    Path: "/uploads",
    FileName: "largefile.zip",
    ContentType: "application/zip",
    ExpiresIn: TimeSpan.FromHours(1),
    MaxSize: 1024 * 1024 * 100 // 100 MB
);

var url = await _fileService.GeneratePresignedUploadUrlAsync(request);

// Client uploads directly to storage using this URL
return Ok(new { uploadUrl = url });
```

### File Lifecycle Management

```csharp
// Webhook handler receives upload notification from object storage
[HttpPost("webhooks/file-uploaded")]
public async Task<IActionResult> HandleFileUploadedWebhook([FromBody] WebhookPayload payload)
{
    // Create actual metadata from webhook
    var actualMetadata = new StorageObjectMetadata(
        Key: payload.StorageKey,
        Size: payload.Size,
        ETag: payload.ETag,
        ContentType: payload.ContentType,
        LastModified: payload.UploadedAt
    );

    // Validate file using storage key (compares webhook data with database)
    // If validation passes and virus scanning disabled → status = Available
    // If validation passes and virus scanning enabled → status = Uploaded
    // If validation fails → file deleted from storage, status = Rejected
    await _fileService.ValidateFileAsync(payload.StorageKey, actualMetadata);

    return Ok();
}

// Delete file
await _fileService.DeleteFileAsync(fileId);
```

### Batch Operations

```csharp
// Delete multiple files efficiently
var fileIds = new[] { id1, id2, id3 };
await _fileService.DeleteFilesAsync(fileIds);

// Get all pending files for processing
var pendingFiles = await _fileService.GetPendingFilesAsync();
foreach (var file in pendingFiles)
{
    // Process validation or scanning
}
```

### Publishing Domain Events

```csharp
// FileService automatically publishes events when IEventPublisher is registered
public class FileService : IFileService
{
    private readonly IEventPublisher? _eventPublisher;

    public async Task<FileMetadata> UploadFileAsync(UploadRequest request)
    {
        // Upload file and save to database
        var file = await _storage.UploadAsync(request);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain events if publisher is configured
        if (_eventPublisher != null)
        {
            await _eventPublisher.PublishDomainEventsAsync(
                file.DomainEvents,
                cancellationToken);

            file.ClearDomainEvents();
        }

        return file;
    }
}

// Events published:
// - FileUploadedEvent (when file is uploaded)
// - FileValidatedEvent (when file passes validation)
// - FileScannedEvent (when file passes virus scan)
// - FileRejectedEvent (when file is rejected)
// - FileDeletedEvent (when file is deleted)
```

### Consuming Events

```csharp
// Example consumer listening for file upload events
public class FileUploadedEventConsumer : IHostedService
{
    public async Task ProcessEventAsync(FileUploadedEvent @event)
    {
        // Trigger virus scanning
        await _virusScanner.ScanAsync(@event.FileId);

        // Send notification
        await _notificationService.NotifyAsync(
            @event.UserId,
            $"File {@event.FileName} uploaded successfully");

        // Update analytics
        await _analytics.TrackUploadAsync(@event.FileId, @event.Size);
    }
}
```

See [Usage Guide](Docs/USAGE-GUIDE.md) for more examples.

## Architecture

FileManager SDK follows Clean Architecture and Domain-Driven Design principles:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│              (Your API / Application)                    │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│               Application Layer                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IFileService (Orchestration)                     │  │
│  │  - UploadFileAsync()                              │  │
│  │  - DownloadFileAsync()                            │  │
│  │  - ValidateFileAsync()                            │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                  Domain Layer                            │
│  ┌──────────────────────────────────────────────────┐  │
│  │  FileMetadata (Aggregate Root)                    │  │
│  │  - Status Transitions                             │  │
│  │  - Domain Events                                  │  │
│  │  - Business Rules                                 │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              Infrastructure Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ MinIOService │  │SeaweedFsServ.│  │  S3Service   │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  EF Core Repositories + Unit of Work              │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Key Concepts

- **FileMetadata**: Aggregate root managing file lifecycle and status transitions
- **IFileService**: Application service orchestrating file operations
- **IObjectStorage**: Provider-agnostic storage interface
- **IEventPublisher**: User-implemented event handler for responding to file lifecycle events
- **Repository Pattern**: Data access abstraction with Unit of Work
- **Domain Events**: FileUploaded, FileValidated, FileScanned, FileRejected, FileDeleted
- **ValidateFileAsync**: Called by object storage webhooks to validate files by comparing actual metadata (from webhook) with expected metadata (from database); automatically deletes from storage if validation fails and sets status to Available or Uploaded based on virus scanning configuration

## Documentation

### Getting Started
- [Quick Start Guide](Docs/QUICK-START.md)
- [Installation](Docs/INSTALLATION.md)
- [Configuration](Docs/CONFIGURATION.md)

### Guides
- [Usage Guide](Docs/USAGE-GUIDE.md)
- [Database Configuration](Docs/DATABASE-CONFIGURATION.md)
- [Dependency Injection](Docs/DI-USAGE-EXAMPLE.md)
- [Event Handling Implementation](Docs/EVENT-HANDLING-GUIDE.md)
- [Migration Guide](Docs/MIGRATION-GUIDE.md)

### Advanced
- [Architecture Overview](Docs/ARCHITECTURE.md)
- [Domain Events & Event Handling](Docs/EVENT-HANDLING-GUIDE.md)
- [Custom Storage Provider](Docs/CUSTOM-PROVIDER.md)
- [Performance Tuning](Docs/PERFORMANCE.md)

### Reference
- [API Reference](Docs/API-REFERENCE.md)
- [Configuration Reference](Docs/CONFIGURATION.md)
- [Service Lifetime Guide](Docs/SERVICE-LIFETIME-GUIDE.md)

## Supported Providers

| Provider | Status | Description |
|----------|--------|-------------|
| **MinIO** | ✅ Production | S3-compatible self-hosted object storage |
| **SeaweedFS** | 🚧 In Progress | Distributed file system with HTTP API |
| **AWS S3** | 🚧 In Progress | Amazon S3 and S3-compatible services |
| **Azure Blob** | 📋 Planned | Microsoft Azure Blob Storage |
| **Google Cloud** | 📋 Planned | Google Cloud Storage |

## Supported Databases

- SQL Server (2019+)
- PostgreSQL (12+)
- MySQL (8.0+)
- SQLite (3.x)

## Requirements

- .NET 9.0 or later
- One of the supported storage providers
- One of the supported databases

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📧 Email: support@filemanager-sdk.com
- 💬 Discussions: [GitHub Discussions](https://github.com/your-org/filemanager-sdk/discussions)
- 🐛 Issues: [GitHub Issues](https://github.com/your-org/filemanager-sdk/issues)
- 📖 Documentation: [https://docs.filemanager-sdk.com](https://docs.filemanager-sdk.com)

## Acknowledgments

- Built with ❤️ using .NET 9 and Clean Architecture principles
- Inspired by modern cloud-native file management solutions
- Special thanks to all [contributors](CONTRIBUTORS.md)

---

**Made with .NET 9** | **Clean Architecture** | **DDD Principles**
