namespace FileManager.Domain.Events;

/// <summary>
/// Event raised when a file has been deleted.
/// </summary>
public sealed class FileDeletedEvent(Guid fileId, string storageKey) : DomainEvent
{
    public Guid FileId { get; } = fileId;
    public string StorageKey { get; } = storageKey;
}