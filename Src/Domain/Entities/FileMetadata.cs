using FileManager.Domain.Enums;
using FileManager.Domain.Events;
using FileManager.Domain.Exceptions;

namespace FileManager.Domain.Entities;

/// <summary>
/// Aggregate root representing file metadata and lifecycle.
/// Enforces business rules for file status transitions and raises domain events.
/// </summary>
public class FileMetadata : AggregateRoot
{
    private FileMetadata()
    {
        // Required for EF Core
    }

    private FileMetadata(
        string fileName,
        string path,
        long size,
        string contentType,
        string storageKey,
        StorageProvider provider,
        string? hash,
        bool validationEnabled,
        bool virusScanningEnabled)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        Path = path;
        Size = size;
        ContentType = contentType;
        StorageKey = storageKey;
        Provider = provider;
        Hash = hash;
        UploadedAt = DateTime.UtcNow;

        // Determine initial status based on configuration
        if (!validationEnabled && !virusScanningEnabled)
        {
            Status = FileStatus.Available;
        }
        else
        {
            Status = FileStatus.Pending;
        }

        RaiseDomainEvent(new FileUploadedEvent(Id, storageKey, fileName));
    }

    // ============================
    // Properties
    // ============================
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public FileStatus Status { get; private set; }
    public string? Hash { get; private set; }
    public StorageProvider Provider { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public DateTime? ScannedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // ============================
    // Factory Methods
    // ============================

    /// <summary>
    /// Creates a new file metadata instance.
    /// </summary>
    public static FileMetadata Create(
        string fileName,
        string path,
        long size,
        string contentType,
        string storageKey,
        StorageProvider provider,
        string? hash = null,
        bool validationEnabled = true,
        bool virusScanningEnabled = false)
    {
        return new FileMetadata(
            fileName,
            path,
            size,
            contentType,
            storageKey,
            provider,
            hash,
            validationEnabled,
            virusScanningEnabled);
    }

    // ============================
    // State Transition Methods
    // ============================

    /// <summary>
    /// Marks the file as uploaded (validated but not yet scanned).
    /// Transition: Pending → Uploaded
    /// </summary>
    public void MarkAsUploaded()
    {
        ValidateTransition(FileStatus.Uploaded);
        Status = FileStatus.Uploaded;
        RaiseDomainEvent(new FileValidatedEvent(Id, StorageKey));
    }

    /// <summary>
    /// Marks the file as validated and available (no virus scanning required).
    /// Transition: Pending → Available
    /// </summary>
    public void MarkAsValidated()
    {
        ValidateTransition(FileStatus.Available);
        Status = FileStatus.Available;
        ValidatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new FileValidatedEvent(Id, StorageKey));
    }

    /// <summary>
    /// Marks the file as scanned and available (clean scan result).
    /// Transition: Uploaded → Available
    /// </summary>
    public void MarkAsScanned()
    {
        ValidateTransition(FileStatus.Available);
        Status = FileStatus.Available;
        ScannedAt = DateTime.UtcNow;
        RaiseDomainEvent(new FileScannedEvent(Id, StorageKey));
    }

    /// <summary>
    /// Rejects the file with a reason (validation failed or virus detected).
    /// Transition: Any → Rejected
    /// </summary>
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason cannot be empty", nameof(reason));

        Status = FileStatus.Rejected;
        RejectionReason = reason;
        RaiseDomainEvent(new FileRejectedEvent(Id, StorageKey, reason));
    }

    /// <summary>
    /// Marks the file as deleted.
    /// </summary>
    public void MarkAsDeleted()
    {
        RaiseDomainEvent(new FileDeletedEvent(Id, StorageKey));
    }

    // ============================
    // Business Rules
    // ============================

    /// <summary>
    /// Checks if download URL can be generated for this file.
    /// Only available files can have download URLs.
    /// </summary>
    public bool CanGenerateDownloadUrl() => Status == FileStatus.Available;

    /// <summary>
    /// Validates if transition to target status is allowed.
    /// </summary>
    private void ValidateTransition(FileStatus targetStatus)
    {
        List<FileStatus> validTransitions = Status switch
        {
            FileStatus.Pending => [FileStatus.Uploaded, FileStatus.Available, FileStatus.Rejected],
            FileStatus.Uploaded => [FileStatus.Available, FileStatus.Rejected],
            _ => []
        };

        if (!validTransitions.Contains(targetStatus))
            throw new InvalidFileStatusTransitionException(Status, targetStatus);
    }
}