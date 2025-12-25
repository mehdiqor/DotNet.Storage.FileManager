namespace FileManager.Domain.Exceptions;

/// <summary>
/// Base exception for all file manager domain exceptions.
/// </summary>
public class FileManagerException : Exception
{
    public FileManagerException(string message) : base(message)
    {
    }

    public FileManagerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}