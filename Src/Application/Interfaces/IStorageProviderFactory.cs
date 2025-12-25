using FileManager.Domain.Enums;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Factory for creating storage provider instances.
/// Use this when you need to access multiple storage providers in the same application.
/// </summary>
public interface IStorageProviderFactory
{
    /// <summary>
    /// Gets a storage provider instance for the specified provider type.
    /// </summary>
    /// <param name="provider">The storage provider type.</param>
    /// <returns>An instance of the requested storage provider.</returns>
    IObjectStorage GetStorage(StorageProvider provider);

    /// <summary>
    /// Gets the default storage provider configured in FileManagerOptions.
    /// </summary>
    /// <returns>An instance of the default storage provider.</returns>
    IObjectStorage GetDefaultStorage();
}