using FileManager.Domain.Enums;

namespace FileManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a storage provider operation fails.
/// </summary>
public class StorageProviderException : FileManagerException
{
    public StorageProviderException(StorageProvider provider, string operation, string message)
        : base($"{provider} {operation} failed: {message}")
    {
        Provider = provider;
        Operation = operation;
    }

    public StorageProviderException(StorageProvider provider, string operation, string message, Exception innerException)
        : base($"{provider} {operation} failed: {message}", innerException)
    {
        Provider = provider;
        Operation = operation;
    }

    public StorageProvider Provider { get; }
    public string Operation { get; }
}