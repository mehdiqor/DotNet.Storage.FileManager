namespace FileManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a file is not found in storage or database.
/// </summary>
public class FileNotFoundException : FileManagerException
{
    public FileNotFoundException(string storageKey) : base($"File not found: {storageKey}")
    {
        StorageKey = storageKey;
    }

    public FileNotFoundException(Guid fileId) : base($"File not found with ID: {fileId}")
    {
        FileId = fileId;
    }

    public string? StorageKey { get; }
    public Guid? FileId { get; }
}