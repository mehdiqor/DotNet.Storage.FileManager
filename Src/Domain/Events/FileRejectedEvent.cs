namespace FileManager.Domain.Events;

/// <summary>
/// Event raised when a file has been rejected (validation failed or virus detected).
/// </summary>
public sealed class FileRejectedEvent(Guid fileId, string storageKey, string reason) : DomainEvent
{
    public Guid FileId { get; } = fileId;
    public string StorageKey { get; } = storageKey;
    public string Reason { get; } = reason;
}