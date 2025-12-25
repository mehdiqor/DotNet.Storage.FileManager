# Architecture Overview

FileManager SDK is built using Clean Architecture and Domain-Driven Design (DDD) principles, ensuring a maintainable, testable, and scalable codebase.

## Table of Contents

- [Architectural Layers](#architectural-layers)
- [Domain Layer](#domain-layer)
- [Application Layer](#application-layer)
- [Infrastructure Layer](#infrastructure-layer)
- [Design Patterns](#design-patterns)
- [Data Flow](#data-flow)
- [Domain Events](#domain-events)

## Architectural Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│              (Consumer's API / Application)              │
└────────────────────┬────────────────────────────────────┘
                     │ Uses
┌────────────────────▼────────────────────────────────────┐
│               Application Layer                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Services (IFileService)                          │  │
│  │  - Orchestrates workflows                         │  │
│  │  - Validates requests                             │  │
│  │  - Coordinates domain logic                       │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Interfaces                                       │  │
│  │  - IFileService, IObjectStorage                   │  │
│  │  - IRepository, IUnitOfWork                       │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  DTOs & Validators                                │  │
│  │  - UploadRequest, FileDto                         │  │
│  │  - FluentValidation rules                         │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │ Uses
┌────────────────────▼────────────────────────────────────┐
│                  Domain Layer                            │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Entities (Aggregate Roots)                       │  │
│  │  - FileMetadata                                   │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Domain Events                                    │  │
│  │  - FileUploaded, FileValidated, FileScanned       │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Domain Exceptions                                │  │
│  │  - FileNotFoundException, DuplicateFileException  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Enums                                            │  │
│  │  - FileStatus, StorageProvider                    │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │ Implemented by
┌────────────────────▼────────────────────────────────────┐
│              Infrastructure Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ MinIOService │  │SeaweedFsServ.│  │  S3Service   │  │
│  │ (IObjectStor.│  │ (IObjectStor.│  │(IObjectStor.)│  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Data Access (EF Core)                            │  │
│  │  - FileManagerDbContext                           │  │
│  │  - Repositories (IFileMetadataRepository)         │  │
│  │  - Unit of Work (IUnitOfWork)                     │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Dependency Injection                             │  │
│  │  - AddFileManager()                               │  │
│  │  - AddStorageProviderFactory()                    │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Domain Layer

The Domain Layer contains core business logic and is independent of external concerns.

### Entities

**FileMetadata** - Aggregate Root
```csharp
public sealed class FileMetadata
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; }
    public string Path { get; private set; }
    public long Size { get; private set; }
    public FileStatus Status { get; private set; }

    // Domain methods
    public static FileMetadata Create(...)
    public void MarkAsValidated()
    public void MarkAsScanned()
    public void Reject(string reason)
    public void MarkAsDeleted()
    public bool CanGenerateDownloadUrl()

    // Domain events
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }
}
```

### Enums

```csharp
public enum FileStatus
{
    Pending,    // Awaiting validation
    Uploaded,   // Validated, awaiting scan
    Rejected,   // Validation/scan failed
    Available   // Ready for download
}

public enum StorageProvider
{
    MinIo,
    SeaweedFs,
    S3
}
```

### Domain Events

- **FileUploadedEvent**: Raised when file is uploaded
- **FileValidatedEvent**: Raised when validation succeeds
- **FileScannedEvent**: Raised when virus scan completes
- **FileRejectedEvent**: Raised when file is rejected
- **FileDeletedEvent**: Raised when file is deleted

### Domain Exceptions

- **FileManagerException**: Base exception
- **FileNotFoundException**: File not found
- **DuplicateFileException**: File with same hash exists
- **FileNotAvailableException**: File not ready for download
- **InvalidFileStatusTransitionException**: Invalid status change
- **StorageProviderException**: Storage operation failed

## Application Layer

The Application Layer orchestrates use cases and coordinates between layers.

### Services

**IFileService** - Main application service
```csharp
public interface IFileService
{
    Task<FileMetadata> UploadFileAsync(UploadRequest request);
    Task<PresignedUploadResponse> GeneratePresignedUploadUrlAsync(...);
    Task<Stream> DownloadFileAsync(Guid fileId);
    Task<FileMetadata?> GetFileMetadataAsync(Guid fileId);
    Task DeleteFileAsync(Guid fileId);
    Task MarkAsValidatedAsync(Guid fileId);
    // ... more methods
}
```

### DTOs (Data Transfer Objects)

- **UploadRequest**: File upload parameters
- **PresignedUploadRequest**: Presigned URL request
- **PresignedUploadResponse**: Upload URL with metadata
- **FileDto**: File information with URLs
- **StorageObjectMetadata**: Storage metadata

### Validators (FluentValidation)

- **UploadRequestValidator**: Validates upload requests
  - File size limits
  - Content type whitelist
  - Path and filename validation
  - Hash format validation
- **PresignedUploadRequestValidator**: Validates presigned URL requests

## Infrastructure Layer

The Infrastructure Layer implements interfaces defined in Application layer.

### Storage Providers

All providers implement `IObjectStorage`:

```csharp
public interface IObjectStorage
{
    StorageProvider Provider { get; }
    Task<UploadResult> UploadAsync(UploadRequest request);
    Task<Stream> DownloadAsync(string storageKey);
    Task<string> GetPresignedUploadUrlAsync(PresignedUploadRequest request);
    Task<string> GetPresignedDownloadUrlAsync(string storageKey, TimeSpan expiresIn);
    Task RemoveAsync(string storageKey);
    Task RemoveBatchAsync(IEnumerable<string> storageKeys);
    Task<bool> ExistsAsync(string storageKey);
    Task<StorageObjectMetadata> GetMetadataAsync(string storageKey);
    Task<bool> HealthAsync();
}
```

**Implementations:**
- MinIoService
- SeaweedFsService
- S3Service

### Data Access

**Repository Pattern:**
```csharp
public interface IRepository<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TEntity>> GetAllAsync();
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    // ... more methods
}
```

**Specialized Repository:**
```csharp
public interface IFileMetadataRepository : IRepository<FileMetadata>
{
    Task<FileMetadata?> GetByStorageKeyAsync(string storageKey);
    Task<FileMetadata?> GetByHashAsync(string hash);
    Task<IReadOnlyList<FileMetadata>> GetByStatusAsync(FileStatus status);
    Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync();
    Task<bool> ExistsByHashAsync(string hash);
}
```

**Unit of Work:**
```csharp
public interface IUnitOfWork : IDisposable
{
    IFileMetadataRepository FileMetadata { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### Database Configuration

Entity Framework Core with support for:
- SQL Server
- PostgreSQL
- MySQL
- SQLite

**Entity Configuration:**
```csharp
public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("FileMetadata");
        builder.HasKey(f => f.Id);

        // Indexes for performance
        builder.HasIndex(f => f.StorageKey).IsUnique();
        builder.HasIndex(f => f.Hash);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => new { f.Status, f.UploadedAt });
    }
}
```

## Design Patterns

### 1. **Repository Pattern**
Abstracts data access logic, allowing easy testing and provider swapping.

### 2. **Unit of Work Pattern**
Manages transactions across multiple repository operations.

### 3. **Factory Pattern**
`IStorageProviderFactory` creates storage provider instances dynamically.

### 4. **Strategy Pattern**
Different storage providers implement the same `IObjectStorage` interface.

### 5. **Domain Events**
FileMetadata raises domain events for important state changes.

### 6. **Options Pattern**
Configuration injected via `IOptions<T>` for type-safe settings.

## Data Flow

### Upload Flow

```
1. Client
   ↓ POST /api/files/upload
2. Controller
   ↓ UploadRequest
3. FluentValidation
   ↓ Validated Request
4. FileService
   ├─→ Check duplicate (Repository)
   ├─→ Create FileMetadata (Domain)
   ├─→ Save to DB (Repository + UnitOfWork)
   └─→ Upload to Storage (IObjectStorage)
   ↓ FileMetadata
5. Controller
   ↓ FileDto
6. Client
```

### Download Flow

```
1. Client
   ↓ GET /api/files/{id}/download
2. Controller
   ↓ File ID
3. FileService
   ├─→ Get FileMetadata (Repository)
   ├─→ Validate status (CanGenerateDownloadUrl)
   └─→ Download from Storage (IObjectStorage)
   ↓ Stream
4. Controller
   ↓ File Stream
5. Client
```

### Status Transition Flow

```
FileMetadata Creation
    ↓
[Pending]
    ↓ MarkAsValidated()
[Uploaded] (if virus scanning enabled)
    ↓ MarkAsScanned()
[Available]

OR

[Pending]
    ↓ Reject()
[Rejected]
```

## Domain Events

Events are raised during domain operations and can be published to message brokers:

```csharp
// After file upload
fileMetadata.AddDomainEvent(new FileUploadedEvent(fileMetadata.Id));

// After validation
fileMetadata.AddDomainEvent(new FileValidatedEvent(fileMetadata.Id));

// TODO: Event publishing
// await _eventPublisher.PublishAsync(fileMetadata.DomainEvents);
```

**Planned Event Publishers:**
- RabbitMQ
- Azure Service Bus
- AWS SQS

## Dependency Injection

### Single Provider Registration

```csharp
builder.Services.AddFileManager(builder.Configuration);
```

Registers:
- Selected storage provider (based on config)
- FileService
- Repositories and UnitOfWork
- DbContext with selected database
- FluentValidation validators

### Multi-Provider Registration

```csharp
builder.Services.AddStorageProviderFactory(builder.Configuration);
```

Registers:
- ALL storage providers
- StorageProviderFactory
- All other services

See [DI Usage Guide](DI-USAGE-EXAMPLE.md) for details.

## Testing Strategy

### Unit Tests
- Domain logic (FileMetadata state transitions)
- Validators (FluentValidation rules)
- Service layer (FileService orchestration)

### Integration Tests
- Repository operations
- Database migrations
- Storage provider operations

### End-to-End Tests
- Complete upload/download workflows
- Status transition workflows

## Next Steps

- [Usage Guide](USAGE-GUIDE.md)
- [Domain Events](DOMAIN-EVENTS.md)
- [Custom Storage Provider](CUSTOM-PROVIDER.md)
- [Performance Tuning](PERFORMANCE.md)
