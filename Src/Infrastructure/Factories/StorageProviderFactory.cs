using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Domain.Enums;
using FileManager.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Factories;

/// <summary>
/// Factory implementation for creating storage provider instances.
/// </summary>
public sealed class StorageProviderFactory(
    IOptions<FileManagerOptions> options,
    IServiceProvider serviceProvider
) : IStorageProviderFactory
{
    private readonly FileManagerOptions _options = options.Value;

    /// <inheritdoc />
    public IObjectStorage GetStorage(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.MinIo => serviceProvider.GetRequiredService<MinIoService>(),
            StorageProvider.SeaweedFs => serviceProvider.GetRequiredService<SeaweedFsService>(),
            StorageProvider.S3 => serviceProvider.GetRequiredService<S3Service>(),
            _ => throw new ArgumentException($"Unsupported storage provider: {provider}", nameof(provider))
        };
    }

    /// <inheritdoc />
    public IObjectStorage GetDefaultStorage()
    {
        return GetStorage(_options.DefaultProvider);
    }
}