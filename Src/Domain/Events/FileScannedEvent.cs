namespace FileManager.Domain.Events;

/// <summary>
/// Event raised when a file has been scanned for viruses and is clean.
/// </summary>
public sealed class FileScannedEvent(Guid fileId, string storageKey) : DomainEvent
{
    public Guid FileId { get; } = fileId;
    public string StorageKey { get; } = storageKey;
}