namespace FileManager.Domain.Events;

/// <summary>
/// Event raised when a file has been uploaded to storage.
/// </summary>
public sealed class FileUploadedEvent(Guid fileId, string storageKey, string fileName) : DomainEvent
{
    public Guid FileId { get; } = fileId;
    public string StorageKey { get; } = storageKey;
    public string FileName { get; } = fileName;
}