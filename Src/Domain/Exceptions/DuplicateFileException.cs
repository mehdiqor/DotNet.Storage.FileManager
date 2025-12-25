namespace FileManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to upload a file that already exists (by hash).
/// </summary>
public sealed class DuplicateFileException : FileManagerException
{
    public string Hash { get; }

    public DuplicateFileException(string hash)
        : base($"A file with hash '{hash}' already exists in the system")
    {
        Hash = hash;
    }

    public DuplicateFileException(string hash, Guid existingFileId)
        : base($"A file with hash '{hash}' already exists (File ID: {existingFileId})")
    {
        Hash = hash;
    }
}