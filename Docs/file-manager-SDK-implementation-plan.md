# File Manager SDK Implementation Plan

Transform the SeaweedFS webhook-api into a comprehensive File Manager SDK following DDD and Clean Architecture principles.

---

## Proposed Directory Structure (DDD + Clean Architecture)

```
seaweedfs-webhook-setup/
├── src/
│   ├── FileManager.Domain/                    # Layer 1: Core Business Logic
│   │   ├── Entities/
│   │   │   ├── AggregateRoot.cs
│   │   │   └── FileMetadata.cs                # Aggregate root with status state machine
│   │   ├── ValueObjects/
│   │   │   ├── FileName.cs
│   │   │   ├── FilePath.cs
│   │   │   ├── FileSize.cs
│   │   │   ├── ContentType.cs
│   │   │   ├── FileHash.cs
│   │   │   └── FileStatus.cs                  # Enum with transition rules
│   │   ├── Enums/
│   │   │   ├── StorageProvider.cs             # MinIO, SeaweedFS, S3Compatible
│   │   │   └── HashAlgorithm.cs
│   │   ├── Events/
│   │   │   ├── DomainEvent.cs                 # Base class
│   │   │   ├── FileUploadedEvent.cs
│   │   │   ├── FileUploadConfirmedEvent.cs
│   │   │   ├── FileValidatedEvent.cs
│   │   │   ├── FileScannedEvent.cs
│   │   │   ├── FileRejectedEvent.cs
│   │   │   └── FileDeletedEvent.cs
│   │   └── Exceptions/
│   │       ├── FileManagerException.cs        # Base exception
│   │       ├── InvalidFileStatusTransitionException.cs
│   │       ├── FileNotFoundException.cs
│   │       └── StorageProviderException.cs
│   │
│   ├── FileManager.Application/                # Layer 2: Use Cases & DTOs
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IObjectStorage.cs           # Provider-agnostic storage
│   │   │   │   ├── IFileMetadataRepository.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── IMessageBroker.cs           # RabbitMQ/ServiceBus/SQS
│   │   │   │   ├── IEventPublisher.cs
│   │   │   │   └── IVirusScanningService.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── FileDto.cs
│   │   │   │   ├── UploadRequest.cs
│   │   │   │   ├── UploadResult.cs
│   │   │   │   ├── StorageObjectMetadata.cs
│   │   │   │   ├── PresignedUploadRequest.cs
│   │   │   │   └── ScanResult.cs
│   │   │   ├── Options/
│   │   │   │   └── FileManagerOptions.cs
│   │   │   └── Behaviors/
│   │   │       ├── ValidationBehavior.cs       # MediatR pipeline
│   │   │       └── LoggingBehavior.cs
│   │   ├── Features/
│   │   │   ├── Files/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── UploadFile/
│   │   │   │   │   │   ├── UploadFileCommand.cs
│   │   │   │   │   │   ├── UploadFileCommandHandler.cs
│   │   │   │   │   │   └── UploadFileValidator.cs
│   │   │   │   │   ├── DeleteFile/
│   │   │   │   │   │   ├── DeleteFileCommand.cs
│   │   │   │   │   │   └── DeleteFileCommandHandler.cs
│   │   │   │   │   └── DownloadFile/
│   │   │   │   │       ├── DownloadFileCommand.cs
│   │   │   │   │       └── DownloadFileCommandHandler.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetFile/
│   │   │   │       │   ├── GetFileQuery.cs
│   │   │   │       │   └── GetFileQueryHandler.cs
│   │   │   │       └── ListFiles/
│   │   │   │           ├── ListFilesQuery.cs
│   │   │   │           └── ListFilesQueryHandler.cs
│   │   │   ├── Validation/
│   │   │   │   └── Commands/
│   │   │   │       └── ValidateFile/
│   │   │   │           ├── ValidateFileCommand.cs
│   │   │   │           └── ValidateFileCommandHandler.cs
│   │   │   └── VirusScanning/
│   │   │       └── Commands/
│   │   │           └── ScanFile/
│   │   │               ├── ScanFileCommand.cs
│   │   │               └── ScanFileCommandHandler.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── FileManager.Infrastructure/             # Layer 3: External Implementations
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   └── FileMetadataConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   └── FileMetadataRepository.cs
│   │   │   ├── FileManagerDbContext.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Migrations/
│   │   ├── ObjectStorage/
│   │   │   ├── ObjectStorageBase.cs           # Base provider class
│   │   │   ├── ObjectStorageFactory.cs        # Strategy pattern factory
│   │   │   ├── MinIO/
│   │   │   │   ├── MinIOStorageService.cs
│   │   │   │   └── MinIOOptions.cs
│   │   │   ├── SeaweedFS/
│   │   │   │   ├── SeaweedFSStorageService.cs
│   │   │   │   ├── SeaweedFSOptions.cs
│   │   │   │   └── SeaweedFSUploadResponse.cs
│   │   │   └── S3/
│   │   │       ├── S3StorageService.cs
│   │   │       └── S3Options.cs
│   │   ├── Messaging/
│   │   │   ├── EventPublisher.cs
│   │   │   ├── RabbitMQ/
│   │   │   │   ├── RabbitMQMessageBroker.cs
│   │   │   │   └── RabbitMQOptions.cs
│   │   │   ├── AzureServiceBus/
│   │   │   │   ├── AzureServiceBusMessageBroker.cs
│   │   │   │   └── AzureServiceBusOptions.cs
│   │   │   └── AwsSqs/
│   │   │       ├── AwsSqsMessageBroker.cs
│   │   │       └── AwsSqsOptions.cs
│   │   ├── VirusScanning/
│   │   │   └── ClamAV/
│   │   │       ├── ClamAVScanningService.cs
│   │   │       └── ClamAVOptions.cs
│   │   └── DependencyInjection.cs
│   │
│   └── FileManager.Api/                        # Layer 4: Presentation (Web API)
│       ├── Controllers/
│       │   ├── FilesController.cs              # File management endpoints
│       │   └── WebhooksController.cs           # Storage event webhooks
│       ├── Middleware/
│       │   ├── WebhookAuthenticationMiddleware.cs
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Filters/
│       │   └── ValidationFilter.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       ├── Models/                             # API-specific DTOs
│       │   ├── WebhookResponse.cs
│       │   └── SeaweedFilerEvent.cs            # Keep for webhook compatibility
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Dockerfile
│
├── tests/
│   ├── FileManager.Domain.Tests/
│   │   └── Entities/
│   │       └── FileMetadataTests.cs
│   ├── FileManager.Application.Tests/
│   │   └── Features/
│   │       └── Files/
│   │           └── UploadFileTests.cs
│   ├── FileManager.Infrastructure.Tests/
│   │   ├── Persistence/
│   │   └── ObjectStorage/
│   └── FileManager.Api.Tests/
│       └── Controllers/
│
├── docker-compose.yml                          # Keep existing infrastructure
├── config/
│   ├── filer.toml
│   └── s3.json
└── README.md
```

---

## Implementation Overview

### Goal
Refactor the webhook-api into a multi-provider file management SDK with:
- **3 Storage Providers**: MinIO, SeaweedFS, S3-compatible
- **Metadata Validation**: Compare uploaded files with database records
- **Virus Scanning**: Optional ClamAV integration with status workflow
- **Event-Driven Architecture**: Abstract message broker (RabbitMQ/Azure Service Bus/AWS SQS)
- **Provider-Agnostic Database**: EF Core with SqlServer/PostgreSQL/MySQL/SQLite support

### Status Workflow

**Without validation/scanning:**
- Upload → `Available` immediately

**With validation only:**
- Upload → `Pending` → Validation → `Available` or `Rejected`

**With validation + virus scanning:**
- Upload → `Pending` → Validation → `Uploaded` → Scan → `Available` or `Rejected`

---

## Phase 1: Domain Layer (Core Business Logic)

### 1.1 Create FileMetadata Aggregate Root

**File:** `src/FileManager.Domain/Entities/FileMetadata.cs`

```csharp
public class FileMetadata : AggregateRoot
{
    // Properties as value objects
    public Guid Id { get; private set; }
    public FileName FileName { get; private set; }
    public FilePath Path { get; private set; }
    public FileSize Size { get; private set; }
    public ContentType ContentType { get; private set; }
    public FileStatus Status { get; private set; }
    public FileHash Hash { get; private set; }
    public StorageProvider Provider { get; private set; }
    public string StorageKey { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public DateTime? ScannedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Factory method
    public static FileMetadata Create(...)
    {
        // Determine initial status based on config
        // Raise FileUploadedEvent
    }

    // State transition methods (enforce business rules)
    public void MarkAsUploaded() { }
    public void MarkAsValidated() { }
    public void MarkAsScanned() { }
    public void Reject(string reason) { }
    public bool CanGenerateDownloadUrl() => Status == FileStatus.Available;
}
```

**Key Points:**
- All state changes through methods (not direct property setters)
- Status transitions validated using `FileStatusTransitions.CanTransition()`
- Raises domain events on state changes
- Never use `!` operator - use `?.` and `??` for nullability

### 1.2 Create Value Objects

**Files:**
- `src/FileManager.Domain/ValueObjects/FileName.cs`
- `src/FileManager.Domain/ValueObjects/FilePath.cs`
- `src/FileManager.Domain/ValueObjects/FileSize.cs`
- `src/FileManager.Domain/ValueObjects/ContentType.cs`
- `src/FileManager.Domain/ValueObjects/FileHash.cs`

All use `record` syntax with validation in constructor:

```csharp
public record FileName
{
    public string Value { get; }

    public FileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("File name cannot be empty");
        if (value.Length > 255)
            throw new ArgumentException("File name too long");

        Value = value;
    }
}
```

### 1.3 Create Domain Events

**Files:**
- `src/FileManager.Domain/Events/FileUploadedEvent.cs`
- `src/FileManager.Domain/Events/FileUploadConfirmedEvent.cs`
- `src/FileManager.Domain/Events/FileValidatedEvent.cs`
- `src/FileManager.Domain/Events/FileScannedEvent.cs`
- `src/FileManager.Domain/Events/FileRejectedEvent.cs`

All inherit from `DomainEvent` base class with EventId and OccurredAt.

---

## Phase 2: Application Layer (Use Cases & Interfaces)

### 2.1 Define Core Interfaces

**File:** `src/FileManager.Application/Common/Interfaces/IObjectStorage.cs`

```csharp
public interface IObjectStorage
{
    // Upload
    Task<UploadResult> UploadAsync(UploadRequest request, CancellationToken ct);
    Task<string> GetPresignedUploadUrlAsync(PresignedUploadRequest request, CancellationToken ct);

    // Download
    Task<Stream> DownloadAsync(string storageKey, CancellationToken ct);
    Task<string> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan expiresIn, CancellationToken ct);

    // Remove
    Task RemoveAsync(string storageKey, CancellationToken ct);
    Task RemoveBatchAsync(IEnumerable<string> storageKeys, CancellationToken ct);

    // Metadata
    Task<StorageObjectMetadata> GetMetadataAsync(string storageKey, CancellationToken ct);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct);

    StorageProvider Provider { get; }
}
```

**Other Interfaces:**
- `IFileMetadataRepository` - CRUD operations for FileMetadata
- `IUnitOfWork` - Transaction management + repository access
- `IMessageBroker` - Abstract message broker (RabbitMQ/ServiceBus/SQS)
- `IEventPublisher` - Publishes domain events to message broker
- `IVirusScanningService` - Scan files with ClamAV or custom implementation

### 2.2 Implement Commands & Queries (MediatR)

**Upload Command:**

`src/FileManager.Application/Features/Files/Commands/UploadFile/UploadFileCommandHandler.cs`

```csharp
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IObjectStorage _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly FileManagerOptions _options;

    public async Task<UploadFileResult> Handle(...)
    {
        // 1. Upload to storage provider
        var uploadResult = await _storage.UploadAsync(...);

        // 2. Create domain entity
        var fileMetadata = FileMetadata.Create(...);

        // 3. Save to database
        await _unitOfWork.FileMetadata.AddAsync(fileMetadata);
        await _unitOfWork.SaveChangesAsync();

        // 4. Publish domain events
        await _eventPublisher.PublishDomainEventsAsync(fileMetadata.DomainEvents);

        // 5. Generate download URL if available
        string? downloadUrl = null;
        if (fileMetadata.CanGenerateDownloadUrl())
        {
            downloadUrl = await _storage.GetPresignedDownloadUrlAsync(...);
        }

        return new UploadFileResult(fileMetadata.Id, uploadResult.StorageKey, fileMetadata.Status, downloadUrl);
    }
}
```

**Validation Command:**

`src/FileManager.Application/Features/Validation/Commands/ValidateFile/ValidateFileCommandHandler.cs`

```csharp
public async Task<ValidationResult> Handle(...)
{
    // 1. Get file metadata from database
    var fileMetadata = await _unitOfWork.FileMetadata.GetByStorageKeyAsync(...);

    // 2. Get actual metadata from storage
    var storageMetadata = await _storage.GetMetadataAsync(...);

    // 3. Compare hash and size
    var isValid = storageMetadata.Size == expectedSize &&
                 storageMetadata.ETag == expectedHash;

    // 4. Update status
    if (isValid)
        fileMetadata.MarkAsValidated();
    else
        fileMetadata.Reject("Metadata mismatch");

    await _unitOfWork.SaveChangesAsync();

    // 5. Publish events
    await _eventPublisher.PublishDomainEventsAsync(fileMetadata.DomainEvents);

    return new ValidationResult(isValid);
}
```

**Virus Scan Command:**

`src/FileManager.Application/Features/VirusScanning/Commands/ScanFile/ScanFileCommandHandler.cs`

Similar pattern: download → scan → update status → publish events.

**Get File Query:**

`src/FileManager.Application/Features/Files/Queries/GetFile/GetFileQueryHandler.cs`

```csharp
public async Task<FileDto?> Handle(...)
{
    var fileMetadata = await _unitOfWork.FileMetadata.GetByIdAsync(...);

    string? downloadUrl = null;
    if (fileMetadata?.CanGenerateDownloadUrl() == true)
    {
        downloadUrl = await _storage.GetPresignedDownloadUrlAsync(...);
    }

    return new FileDto(..., downloadUrl);
}
```

---

## Phase 3: Infrastructure Layer (External Implementations)

### 3.1 Database (EF Core)

**DbContext:** `src/FileManager.Infrastructure/Persistence/FileManagerDbContext.cs`

```csharp
public class FileManagerDbContext : DbContext
{
    public DbSet<FileMetadata> Files { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileManagerDbContext).Assembly);
    }
}
```

**Entity Configuration:** `src/FileManager.Infrastructure/Persistence/Configurations/FileMetadataConfiguration.cs`

```csharp
public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("Files");
        builder.HasKey(f => f.Id);

        // Convert value objects to/from primitives
        builder.Property(f => f.FileName)
            .HasConversion(v => v.Value, v => new FileName(v))
            .HasMaxLength(255);

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(f => f.StorageKey).IsUnique();
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => new { f.Path, f.FileName });

        builder.Ignore(f => f.DomainEvents);
    }
}
```

**Repository:** `src/FileManager.Infrastructure/Persistence/Repositories/FileMetadataRepository.cs`

Standard EF Core repository implementation with async methods.

**Unit of Work:** `src/FileManager.Infrastructure/Persistence/UnitOfWork.cs`

Coordinates repositories and transactions.

### 3.2 Object Storage Providers

**Base Class:** `src/FileManager.Infrastructure/ObjectStorage/ObjectStorageBase.cs`

```csharp
public abstract class ObjectStorageBase : IObjectStorage
{
    protected readonly ILogger Logger;
    protected readonly string BucketName;

    public abstract StorageProvider Provider { get; }

    protected string BuildStorageKey(string path, string fileName)
    {
        return $"{path.TrimStart('/')}/{fileName}".TrimStart('/');
    }
}
```

**MinIO Implementation:** `src/FileManager.Infrastructure/ObjectStorage/MinIO/MinIOStorageService.cs`

```csharp
public class MinIOStorageService : ObjectStorageBase
{
    private readonly IMinioClient _client;

    public override StorageProvider Provider => StorageProvider.MinIO;

    public override async Task<UploadResult> UploadAsync(...)
    {
        var storageKey = BuildStorageKey(request.Path, request.FileName);

        var args = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(storageKey)
            .WithStreamData(request.Content)
            .WithContentType(request.ContentType);

        var response = await _client.PutObjectAsync(args, cancellationToken);

        return new UploadResult(storageKey, response.Etag, response.Size, response.VersionId);
    }

    public override async Task<string> GetPresignedDownloadUrlAsync(...)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(storageKey)
            .WithExpiry((int)expiresIn.TotalSeconds);

        return await _client.PresignedGetObjectAsync(args);
    }
}
```

**SeaweedFS Implementation:** `src/FileManager.Infrastructure/ObjectStorage/SeaweedFS/SeaweedFSStorageService.cs`

Uses HttpClient to POST to filer. For presigned URLs, generates HMAC-signed URLs since SeaweedFS doesn't have native presigned URL support.

**S3 Implementation:** `src/FileManager.Infrastructure/ObjectStorage/S3/S3StorageService.cs`

Uses AWS SDK (`IAmazonS3`) with native presigned URL support.

**Factory:** `src/FileManager.Infrastructure/ObjectStorage/ObjectStorageFactory.cs`

```csharp
public class ObjectStorageFactory : IObjectStorageFactory
{
    public IObjectStorage GetStorage(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.MinIO => _serviceProvider.GetRequiredService<MinIOStorageService>(),
            StorageProvider.SeaweedFS => _serviceProvider.GetRequiredService<SeaweedFSStorageService>(),
            StorageProvider.S3Compatible => _serviceProvider.GetRequiredService<S3StorageService>(),
            _ => throw new NotSupportedException($"Provider {provider} not supported")
        };
    }
}
```

### 3.3 Message Brokers

**RabbitMQ:** `src/FileManager.Infrastructure/Messaging/RabbitMQ/RabbitMQMessageBroker.cs`

Port existing `RabbitMQService.cs` logic, implement `IMessageBroker`.

**Azure Service Bus:** `src/FileManager.Infrastructure/Messaging/AzureServiceBus/AzureServiceBusMessageBroker.cs`

Use `ServiceBusClient` from Azure SDK.

**AWS SQS:** `src/FileManager.Infrastructure/Messaging/AwsSqs/AwsSqsMessageBroker.cs`

Use `IAmazonSQS` from AWS SDK.

**Event Publisher:** `src/FileManager.Infrastructure/Messaging/EventPublisher.cs`

```csharp
public class EventPublisher : IEventPublisher
{
    private readonly IMessageBroker _broker;

    public async Task PublishDomainEventsAsync(IEnumerable<DomainEvent> events, ...)
    {
        foreach (var domainEvent in events)
        {
            await _broker.PublishAsync(domainEvent, cancellationToken: ct);
        }
    }
}
```

### 3.4 Virus Scanning

**ClamAV:** `src/FileManager.Infrastructure/VirusScanning/ClamAV/ClamAVScanningService.cs`

```csharp
public class ClamAVScanningService : IVirusScanningService
{
    private readonly IClamClient _clamClient;

    public async Task<ScanResult> ScanAsync(Stream content, ...)
    {
        var result = await _clamClient.SendAndScanFileAsync(content, ct);
        var isClean = result.Result == ClamScanResults.Clean;

        return new ScanResult(isClean, result.InfectedFiles?.FirstOrDefault()?.VirusName);
    }
}
```

### 3.5 Dependency Injection

**File:** `src/FileManager.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
{
    services.AddDatabase(config);
    services.AddObjectStorage(config);
    services.AddMessageBroker(config);
    services.AddVirusScanning(config);
    return services;
}

private static IServiceCollection AddDatabase(...)
{
    var provider = config.GetValue<string>("Database:Provider");
    var connectionString = config.GetConnectionString(provider);

    services.AddDbContext<FileManagerDbContext>(options =>
    {
        switch (provider)
        {
            case "SqlServer": options.UseSqlServer(connectionString); break;
            case "PostgreSQL": options.UseNpgsql(connectionString); break;
            case "MySQL": options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)); break;
            case "SQLite": options.UseSqlite(connectionString); break;
        }
    });

    services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    return services;
}

private static IServiceCollection AddObjectStorage(...)
{
    // Register all provider-specific clients
    services.AddSingleton<IMinioClient>(...);
    services.AddHttpClient<SeaweedFSStorageService>();
    services.AddSingleton<IAmazonS3>(...);

    // Register services
    services.AddScoped<MinIOStorageService>();
    services.AddScoped<SeaweedFSStorageService>();
    services.AddScoped<S3StorageService>();

    // Register factory and default provider
    services.AddScoped<IObjectStorageFactory, ObjectStorageFactory>();
    services.AddScoped<IObjectStorage>(sp => sp.GetRequiredService<IObjectStorageFactory>().GetStorage());

    return services;
}

private static IServiceCollection AddMessageBroker(...)
{
    var provider = config.GetValue<string>("MessageBroker:Provider");

    switch (provider)
    {
        case "RabbitMQ":
            services.AddSingleton<IMessageBroker, RabbitMQMessageBroker>();
            break;
        case "AzureServiceBus":
            services.AddSingleton<IMessageBroker, AzureServiceBusMessageBroker>();
            break;
        case "AwsSqs":
            services.AddSingleton<IMessageBroker, AwsSqsMessageBroker>();
            break;
    }

    services.AddScoped<IEventPublisher, EventPublisher>();

    return services;
}
```

---

## Phase 4: API Layer (Presentation)

### 4.1 Migrate Existing Controllers

**File:** `src/FileManager.Api/Controllers/WebhooksController.cs`

Port logic from existing `SeaweedFsWebhookController.cs`:

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("storage-event")]
    public async Task<IActionResult> HandleStorageEvent([FromBody] SeaweedFilerEvent seaweedEvent)
    {
        // Convert SeaweedFilerEvent to ValidateFileCommand
        var command = new ValidateFileCommand(
            seaweedEvent.FullPath,
            seaweedEvent.NewEntry?.Attributes?.Md5 ?? "",
            seaweedEvent.NewEntry?.Attributes?.FileSize ?? 0
        );

        var result = await _mediator.Send(command);

        return Ok(new WebhookResponse { Success = result.IsValid });
    }
}
```

**File:** `src/FileManager.Api/Controllers/FilesController.cs`

New controller for file management:

```csharp
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string path = "uploads")
    {
        using var stream = file.OpenReadStream();
        var command = new UploadFileCommand(stream, path, file.FileName, file.ContentType);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var query = new GetFileQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var command = new DeleteFileCommand(id);
        await _mediator.Send(command);

        return NoContent();
    }
}
```

### 4.2 Migrate Middleware

**File:** `src/FileManager.Api/Middleware/WebhookAuthenticationMiddleware.cs`

Port existing middleware as-is (already well-implemented).

### 4.3 Program.cs

**File:** `src/FileManager.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add layers
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FileManagerDbContext>();

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FileManagerDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<WebhookAuthenticationMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### 4.4 Configuration

**File:** `src/FileManager.Api/appsettings.json`

```json
{
  "FileManager": {
    "DefaultProvider": "SeaweedFS",
    "ValidationEnabled": true,
    "VirusScanningEnabled": true,
    "PresignedUrlExpiration": "01:00:00"
  },

  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false
    },
    "SeaweedFS": {
      "FilerUrl": "http://localhost:8888",
      "BucketName": "files",
      "SecretKey": "your-secret-key",
      "UseDirectUrls": false
    },
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "AccessKey": "YOUR_ACCESS_KEY",
      "SecretKey": "YOUR_SECRET_KEY",
      "BucketName": "files",
      "Region": "us-east-1"
    }
  },

  "Database": {
    "Provider": "SqlServer",
    "ConnectionStrings": {
      "SqlServer": "Server=localhost;Database=FileManager;Trusted_Connection=True;",
      "PostgreSQL": "Host=localhost;Database=filemanager;Username=postgres;Password=postgres",
      "MySQL": "Server=localhost;Database=filemanager;User=root;Password=root;",
      "SQLite": "Data Source=filemanager.db"
    }
  },

  "MessageBroker": {
    "Provider": "RabbitMQ",
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "seaweed",
      "Password": "seaweed123",
      "VirtualHost": "/",
      "Exchange": "filemanager",
      "Queue": "filemanager_events",
      "RoutingKey": "file.#"
    }
  },

  "VirusScanning": {
    "ClamAV": {
      "Host": "localhost",
      "Port": 3310
    }
  },

  "Webhook": {
    "SecretKey": "your-super-secret-key-change-in-production",
    "ValidateSignature": true
  }
}
```

---

## Event Flow

### Upload → Validation → Available

```
1. Client uploads file
   ↓
2. UploadFileCommandHandler:
   - Uploads to storage provider
   - Creates FileMetadata (Status=Pending if validation enabled)
   - Saves to database
   - Publishes FileUploadedEvent
   ↓
3. Storage provider webhook triggers (SeaweedFS/MinIO event)
   ↓
4. WebhooksController receives event
   ↓
5. ValidateFileCommandHandler:
   - Gets file metadata from database
   - Gets actual metadata from storage
   - Compares hash and size
   - Updates status to Available or Rejected
   - Publishes FileValidatedEvent or FileRejectedEvent
   ↓
6. Client requests file info (GetFileQuery)
   - If Status=Available: returns download URL
   - If Status≠Available: returns null download URL
```

### Upload → Validation → Scan → Available

```
1-5. Same as above (Validation updates to Uploaded instead of Available)
   ↓
6. FileUploadConfirmedEvent published
   ↓
7. ScanFileCommandHandler:
   - Downloads file from storage
   - Scans with ClamAV
   - Updates status to Available (clean) or Rejected (virus)
   - Publishes FileScannedEvent or FileRejectedEvent
```

---

## Migration from Current Structure

### Files to Migrate

| Current File | New Location | Changes |
|-------------|--------------|---------|
| `webhook-api/Models/SeaweedFilerEvent.cs` | `FileManager.Api/Models/SeaweedFilerEvent.cs` | Keep for webhook compatibility |
| `webhook-api/Middleware/WebhookAuthenticationMiddleware.cs` | `FileManager.Api/Middleware/WebhookAuthenticationMiddleware.cs` | Port as-is |
| `webhook-api/Services/RabbitMQService.cs` | `FileManager.Infrastructure/Messaging/RabbitMQ/RabbitMQMessageBroker.cs` | Refactor to implement IMessageBroker |
| `webhook-api/Services/SeaweedEventHandler.cs` | Replaced by MediatR command handlers | Delete |
| `webhook-api/Controllers/SeaweedFsWebhookController.cs` | `FileManager.Api/Controllers/WebhooksController.cs` | Refactor to use MediatR |
| `webhook-api/Configuration/Options.cs` | Split into multiple option classes | Delete |

### Files to Delete

- `webhook-api/Program.cs` - replaced by new Program.cs
- `webhook-api/Services/SeaweedEventHandler.cs` - replaced by command handlers
- `webhook-api/Configuration/Options.cs` - replaced by specific option classes

---

## Critical Implementation Rules

### 1. Nullability Handling

**NEVER use `!` operator.** Always use:
- `?.` null-conditional operator
- `?? defaultValue` null-coalescing operator
- Explicit null checks: `if (value != null)`
- `.OfType<T>()` for collections

```csharp
// BAD
var fileName = metadata.FileName!.Value;

// GOOD
var fileName = metadata.FileName?.Value ?? "unknown";

// GOOD
if (metadata.FileName != null)
{
    var fileName = metadata.FileName.Value;
}
```

### 2. Status Transitions

All status transitions MUST go through domain methods:

```csharp
// BAD
fileMetadata.Status = FileStatus.Available;

// GOOD
fileMetadata.MarkAsValidated();
```

### 3. Domain Events

Always publish domain events after saving:

```csharp
await _unitOfWork.SaveChangesAsync();
await _eventPublisher.PublishDomainEventsAsync(entity.DomainEvents);
```

### 4. Provider Abstraction

All storage operations through `IObjectStorage`:

```csharp
// Use default provider
var storage = serviceProvider.GetRequiredService<IObjectStorage>();

// Explicit provider selection
var factory = serviceProvider.GetRequiredService<IObjectStorageFactory>();
var minioStorage = factory.GetStorage(StorageProvider.MinIO);
```

---

## Testing Strategy

### Unit Tests

```csharp
// FileManager.Domain.Tests/Entities/FileMetadataTests.cs
[Fact]
public void Create_WithValidationDisabled_ShouldSetStatusToAvailable()
{
    var metadata = FileMetadata.Create(..., validationEnabled: false, virusScanningEnabled: false);

    metadata.Status.Should().Be(FileStatus.Available);
    metadata.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<FileUploadedEvent>();
}

[Fact]
public void MarkAsValidated_FromPending_ShouldSucceed()
{
    var metadata = FileMetadata.Create(...);

    metadata.MarkAsValidated();

    metadata.Status.Should().Be(FileStatus.Available);
    metadata.ValidatedAt.Should().NotBeNull();
}

[Fact]
public void MarkAsValidated_FromRejected_ShouldThrowException()
{
    var metadata = FileMetadata.Create(...);
    metadata.Reject("Test");

    metadata.Invoking(m => m.MarkAsValidated())
        .Should().Throw<InvalidFileStatusTransitionException>();
}
```

### Integration Tests

```csharp
// FileManager.Application.Tests/Features/Files/UploadFileTests.cs
[Fact]
public async Task Handle_ValidUpload_ShouldCreateFileMetadata()
{
    var command = new UploadFileCommand(stream, "uploads", "test.txt", "text/plain");

    var result = await _mediator.Send(command);

    result.FileId.Should().NotBeEmpty();
    var fileFromDb = await _context.Files.FindAsync(result.FileId);
    fileFromDb.Should().NotBeNull();
    fileFromDb.FileName.Value.Should().Be("test.txt");
}
```

---

## Implementation Order

### Week 1-2: Foundation
1. Create solution structure (4 projects)
2. Implement Domain layer (entities, value objects, events)
3. Define Application layer interfaces

### Week 3-4: Infrastructure
1. Database setup (DbContext, configurations, repositories)
2. Object storage providers (MinIO, SeaweedFS, S3)
3. Message broker implementations
4. Virus scanning integration

### Week 5: API
1. Migrate controllers and middleware
2. Configure DI in Program.cs
3. Set up appsettings.json

### Week 6: Event Handlers
1. Implement domain event handlers
2. Wire up message broker consumers
3. Test event flow end-to-end

### Week 7-8: Testing & Polish
1. Unit tests for domain logic
2. Integration tests for commands/queries
3. Documentation

---

## Critical Files to Create First

1. **`src/FileManager.Domain/Entities/FileMetadata.cs`**
   - Core aggregate root defining business logic
   - Foundation for entire domain model

2. **`src/FileManager.Application/Common/Interfaces/IObjectStorage.cs`**
   - Primary abstraction for storage providers
   - Drives entire storage strategy

3. **`src/FileManager.Infrastructure/Persistence/FileManagerDbContext.cs`**
   - Database context and entity configurations
   - Critical for data persistence

4. **`src/FileManager.Application/Features/Files/Commands/UploadFile/UploadFileCommandHandler.cs`**
   - Primary use case orchestrating upload workflow
   - Demonstrates layer interaction

5. **`src/FileManager.Infrastructure/DependencyInjection.cs`**
   - Infrastructure service registration
   - Wires all components together

---

## Success Criteria

- [ ] Can upload files to MinIO, SeaweedFS, or S3 via single interface
- [ ] File metadata stored in database with correct initial status
- [ ] Validation workflow updates status based on storage events
- [ ] Virus scanning (when enabled) transitions through Uploaded → Available
- [ ] Download URLs only generated for Available files
- [ ] Events published to configurable message broker (RabbitMQ/ServiceBus/SQS)
- [ ] Users can switch providers via appsettings.json
- [ ] Users can disable validation/scanning via config
- [ ] No `!` operator used anywhere (nullable reference types)
- [ ] All status transitions validated through domain methods
