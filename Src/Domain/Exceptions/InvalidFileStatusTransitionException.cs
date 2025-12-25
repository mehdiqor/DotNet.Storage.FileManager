using FileManager.Domain.Enums;

namespace FileManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when an invalid file status transition is attempted.
/// </summary>
public class InvalidFileStatusTransitionException(FileStatus currentStatus, FileStatus targetStatus)
    : FileManagerException($"Cannot transition from {currentStatus} to {targetStatus}")
{
    public FileStatus CurrentStatus { get; } = currentStatus;
    public FileStatus TargetStatus { get; } = targetStatus;
}