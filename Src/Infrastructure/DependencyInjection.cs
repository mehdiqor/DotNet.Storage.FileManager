using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using FileManager.Application.Validators;
using FileManager.Common.Options;
using FileManager.Domain.Enums;
using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Factories;
using FileManager.Infrastructure.Persistence;
using FileManager.Infrastructure.Persistence.Repositories;
using FileManager.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileManager.Infrastructure;

/// <summary>
/// Extension methods for registering File Manager SDK services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all File Manager SDK services including Application and Infrastructure layers.
    /// Only the selected storage provider (based on FileManagerOptions.DefaultProvider) is registered.
    /// </summary>
    public static IServiceCollection AddFileManager(this IServiceCollection services, IConfiguration configuration)
    {
        // Register shared services (options, validators, application services)
        AddSharedServices(services, configuration);

        // Get the selected provider from configuration
        var defaultProvider = configuration
            .GetSection(FileManagerOptions.SectionName)
            .GetValue<StorageProvider>(nameof(FileManagerOptions.DefaultProvider));

        // Register ONLY the selected storage provider's options and service
        switch (defaultProvider)
        {
            case StorageProvider.MinIo:
                services.Configure<MinIoOptions>(configuration.GetSection(MinIoOptions.SectionName));
                services.AddSingleton<MinIoService>();
                services.AddSingleton<IObjectStorage, MinIoService>();
                break;

            case StorageProvider.SeaweedFs:
                services.Configure<SeaweedFsOptions>(configuration.GetSection(SeaweedFsOptions.SectionName));
                services.AddSingleton<SeaweedFsService>();
                services.AddSingleton<IObjectStorage, SeaweedFsService>();
                break;

            case StorageProvider.S3:
                services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
                services.AddSingleton<S3Service>();
                services.AddSingleton<IObjectStorage, S3Service>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported storage provider: {defaultProvider}. " +
                    $"Valid options are: {string.Join(", ", Enum.GetNames<StorageProvider>())}");
        }

        return services;
    }

    /// <summary>
    /// Registers a storage provider factory for advanced scenarios where multiple providers are needed.
    /// This registers ALL storage providers, not just the default one.
    /// Use this ONLY when you need to work with multiple storage providers simultaneously.
    /// </summary>
    public static IServiceCollection AddStorageProviderFactory(this IServiceCollection services, IConfiguration configuration)
    {
        // Register shared services (options, validators, application services)
        AddSharedServices(services, configuration);

        // Register ALL provider options (needed for factory to resolve any provider)
        services.Configure<MinIoOptions>(configuration.GetSection(MinIoOptions.SectionName));
        services.Configure<SeaweedFsOptions>(configuration.GetSection(SeaweedFsOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        // Register ALL storage service implementations
        services.AddSingleton<MinIoService>();
        services.AddSingleton<SeaweedFsService>();
        services.AddSingleton<S3Service>();

        // Register the factory
        services.AddSingleton<IStorageProviderFactory, StorageProviderFactory>();

        return services;
    }

    /// <summary>
    /// Registers shared services that are common to both single-provider and multi-provider scenarios.
    /// This includes configuration options, application services, validators, and infrastructure services.
    /// </summary>
    private static void AddSharedServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register FileManager options
        services.Configure<FileManagerOptions>(
            configuration.GetSection(FileManagerOptions.SectionName));

        // Register HttpClient for SeaweedFS
        services.AddHttpClient("SeaweedFS");

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<UploadRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<PresignedUploadRequestValidator>();

        // Register database context and repositories
        AddDatabase(services, configuration);

        // Register application services
        services.AddScoped<IFileService, FileService>();

        // TODO: Register message broker
        // services.AddSingleton<IMessageBroker, RabbitMQMessageBroker>();
        // services.AddScoped<IEventPublisher, EventPublisher>();
    }

    /// <summary>
    /// Registers the database context and repositories based on the configured database provider.
    /// Supports SQL Server, PostgreSQL, MySQL, and SQLite.
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        // Register database options
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Register DbContext with the appropriate database provider
        services.AddDbContext<FileManagerDbContext>(options =>
        {
            var connectionString = databaseOptions.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is not configured. " +
                    $"Please set '{DatabaseOptions.SectionName}:ConnectionString' in your configuration.");
            }

            // Configure database provider
            switch (databaseOptions.Provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: databaseOptions.MaxRetryCount,
                            maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                    });
                    break;

                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: databaseOptions.MaxRetryCount,
                            maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                    });
                    break;

                case DatabaseProvider.MySql:
                    options.UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString),
                        mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(
                                maxRetryCount: databaseOptions.MaxRetryCount,
                                maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                                errorNumbersToAdd: null);
                            mySqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                        });
                    break;

                case DatabaseProvider.SqLite:
                    options.UseSqlite(connectionString, sqliteOptions => { sqliteOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds); });
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider: {databaseOptions.Provider}. " +
                        $"Valid options are: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}");
            }

            // Development settings
            if (databaseOptions.EnableDetailedErrors)
                options.EnableDetailedErrors();

            if (databaseOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        // Register repositories
        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}