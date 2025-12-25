using FileManager.Domain.Enums;

namespace FileManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to download a file that is not in an available status.
/// </summary>
public sealed class FileNotAvailableException(Guid fileId, FileStatus currentStatus)
    : FileManagerException($"File {fileId} is not available for download. Current status: {currentStatus}")
{
    public Guid FileId { get; } = fileId;
    public FileStatus CurrentStatus { get; } = currentStatus;
}