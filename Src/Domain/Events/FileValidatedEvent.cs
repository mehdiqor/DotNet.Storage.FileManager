namespace FileManager.Domain.Events;

/// <summary>
/// Event raised when a file has been validated against storage metadata.
/// </summary>
public sealed class FileValidatedEvent(Guid fileId, string storageKey) : DomainEvent
{
    public Guid FileId { get; } = fileId;
    public string StorageKey { get; } = storageKey;
}