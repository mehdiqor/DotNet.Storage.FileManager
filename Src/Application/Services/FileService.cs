using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using FileManager.Common.Utilities;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FileNotFoundException = FileManager.Domain.Exceptions.FileNotFoundException;

namespace FileManager.Application.Services;

/// <summary>
/// Application service for managing file operations.
/// Orchestrates between storage providers, repositories, and domain logic.
/// </summary>
public sealed class FileService(
    IOptions<FileManagerOptions> options,
    ILogger<FileService> logger,
    IObjectStorage storage,
    IUnitOfWork unitOfWork,
    IEventPublisher? eventPublisher = null
) : IFileService
{
    private readonly FileManagerOptions _options = options.Value;

    public async Task<FileMetadata> UploadFileAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Uploading file: {FileName} to path: {Path}",
            request.FileName,
            request.Path);

        // Check for duplicate by hash if provided
        if (!string.IsNullOrEmpty(request.Hash))
        {
            var existingFile = await unitOfWork.FileMetadata.ExistsByHashAsync(
                request.Hash,
                cancellationToken);

            if (existingFile)
                throw new DuplicateFileException(request.Hash);
        }

        // Generate storage key before upload to ensure metadata exists first
        var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);

        // Create file metadata FIRST (before upload)
        var fileMetadata = FileMetadata.Create(
            fileName: request.FileName,
            path: request.Path,
            size: request.Size,
            contentType: request.ContentType,
            hash: request.Hash,
            provider: storage.Provider,
            storageKey: storageKey,
            validationEnabled: _options.ValidationEnabled,
            virusScanningEnabled: _options.VirusScanningEnabled);

        // Save to database BEFORE uploading to storage
        // This ensures metadata exists when upload webhooks/events fire
        await unitOfWork.FileMetadata.AddAsync(fileMetadata, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "File metadata created with ID: {FileId}, Status: {Status}",
            fileMetadata.Id,
            fileMetadata.Status);

        // Now upload to storage
        var uploadResult = await storage.UploadAsync(request, cancellationToken);

        logger.LogInformation(
            "File uploaded to storage with key: {StorageKey}",
            uploadResult.StorageKey);

        // Publish domain events if event publisher is configured
        if (eventPublisher != null && fileMetadata.DomainEvents.Count > 0)
        {
            await eventPublisher.PublishDomainEventsAsync(
                fileMetadata.DomainEvents,
                cancellationToken);

            fileMetadata.ClearDomainEvents();
        }

        return fileMetadata;
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Generating presigned upload URL for: {FileName}",
            request.FileName);

        // Generate storage key before creating metadata
        var storageKey = StorageKeyGenerator.Build(request.Path, request.FileName);

        // Create file metadata FIRST (before generating presigned URL)
        // Note: Size and Hash are unknown at this point, will be updated after actual upload
        var fileMetadata = FileMetadata.Create(
            fileName: request.FileName,
            path: request.Path,
            size: request.MaxSize ?? 0, // Use MaxSize or 0 as placeholder
            contentType: request.ContentType,
            hash: string.Empty, // Hash unknown until upload completes
            provider: storage.Provider,
            storageKey: storageKey,
            validationEnabled: _options.ValidationEnabled,
            virusScanningEnabled: _options.VirusScanningEnabled);

        // Save to database BEFORE generating presigned URL
        // This ensures metadata exists when client uploads directly
        await unitOfWork.FileMetadata.AddAsync(fileMetadata, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "File metadata created with ID: {FileId} for presigned upload",
            fileMetadata.Id);

        // Now generate presigned upload URL
        var url = await storage.GetPresignedUploadUrlAsync(request, cancellationToken);

        logger.LogInformation(
            "Presigned upload URL generated for: {FileName}",
            request.FileName);

        // TODO: Return both URL and FileMetadata ID so client can update after upload
        // TODO: Add CompletePresignedUpload method to update size/hash after client uploads

        return url;
    }

    public async Task<Stream> DownloadFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading file: {FileId}", fileId);

        var fileMetadata = await GetFileMetadataOrThrowAsync(fileId, cancellationToken);

        if (!fileMetadata.CanGenerateDownloadUrl())
            throw new FileNotAvailableException(fileId, fileMetadata.Status);

        return await storage.DownloadAsync(fileMetadata.StorageKey, cancellationToken);
    }

    public async Task<Stream> DownloadFileByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading file by key: {StorageKey}", storageKey);

        return await storage.DownloadAsync(storageKey, cancellationToken);
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(
        Guid fileId,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Generating presigned download URL for file: {FileId}",
            fileId);

        var fileMetadata = await GetFileMetadataOrThrowAsync(fileId, cancellationToken);

        if (!fileMetadata.CanGenerateDownloadUrl())
            throw new FileNotAvailableException(fileId, fileMetadata.Status);

        return await storage.GetPresignedDownloadUrlAsync(
            fileMetadata.StorageKey,
            expiresIn ?? _options.PresignedUrlExpiration,
            cancellationToken);
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting file metadata: {FileId}", fileId);

        return await unitOfWork.FileMetadata.GetByIdAsync(fileId, cancellationToken);
    }

    public async Task<FileMetadata?> GetFileMetadataByKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting file metadata by key: {StorageKey}", storageKey);

        return await unitOfWork.FileMetadata.GetByStorageKeyAsync(
            storageKey,
            cancellationToken);
    }

    public async Task<FileMetadata?> GetFileMetadataByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting file metadata by hash: {Hash}", hash);

        return await unitOfWork.FileMetadata.GetByHashAsync(hash, cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetFilesByStatusAsync(
        FileStatus status,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting files by status: {Status}", status);

        return await unitOfWork.FileMetadata.GetByStatusAsync(status, cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetPendingFilesAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting pending files");

        return await unitOfWork.FileMetadata.GetPendingFilesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting file: {FileId}", fileId);

        var fileMetadata = await GetFileMetadataOrThrowAsync(fileId, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Delete from storage
            await storage.RemoveAsync(fileMetadata.StorageKey, cancellationToken);

            logger.LogInformation(
                "File deleted from storage: {StorageKey}",
                fileMetadata.StorageKey);

            // Mark as deleted in database
            fileMetadata.MarkAsDeleted();
            unitOfWork.FileMetadata.Update(fileMetadata);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation(
                "File deleted successfully: {FileId}",
                fileId);

            // Publish domain events if event publisher is configured
            if (eventPublisher != null && fileMetadata.DomainEvents.Count > 0)
            {
                await eventPublisher.PublishDomainEventsAsync(
                    fileMetadata.DomainEvents,
                    cancellationToken);

                fileMetadata.ClearDomainEvents();
            }
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteFilesAsync(
        IEnumerable<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        var fileIdList = fileIds.ToList();
        logger.LogInformation(
            "Deleting {Count} files",
            fileIdList.Count);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var storageKeys = new List<string>();
            var fileMetadataList = new List<FileMetadata>();

            foreach (var fileId in fileIdList)
            {
                var fileMetadata = await GetFileMetadataOrThrowAsync(
                    fileId,
                    cancellationToken);

                storageKeys.Add(fileMetadata.StorageKey);
                fileMetadata.MarkAsDeleted();
                fileMetadataList.Add(fileMetadata);
            }

            // Delete from storage in batch
            await storage.RemoveBatchAsync(storageKeys, cancellationToken);

            logger.LogInformation(
                "Files deleted from storage: {Count}",
                storageKeys.Count);

            // Update database
            unitOfWork.FileMetadata.UpdateRange(fileMetadataList);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation(
                "Files deleted successfully: {Count}",
                fileIdList.Count);

            // Publish domain events if event publisher is configured
            if (eventPublisher != null)
            {
                foreach (var file in fileMetadataList.Where(file => file.DomainEvents.Count > 0))
                {
                    await eventPublisher.PublishDomainEventsAsync(
                        file.DomainEvents,
                        cancellationToken);

                    file.ClearDomainEvents();
                }
            }
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ValidateFileAsync(
        string storageKey,
        StorageObjectMetadata actualMetadata,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Validating file with storage key: {StorageKey} using actual metadata from webhook",
            storageKey);

        // Look up file metadata by storage key
        var fileMetadata = await unitOfWork.FileMetadata.GetByStorageKeyAsync(
            storageKey,
            cancellationToken);

        if (fileMetadata == null)
        {
            logger.LogWarning("File not found for storage key: {StorageKey}", storageKey);
            throw new FileNotFoundException(storageKey);
        }

        // Check if file is in correct status for validation
        if (fileMetadata.Status != FileStatus.Pending)
        {
            logger.LogWarning(
                "File {FileId} cannot be validated. Current status: {Status}",
                fileMetadata.Id,
                fileMetadata.Status);
            return;
        }

        // Check if validation is enabled
        if (!_options.ValidationEnabled)
        {
            logger.LogWarning("Validation is disabled in configuration");

            // If validation is disabled, skip directly to final status
            if (_options.VirusScanningEnabled)
            {
                // Need virus scanning - mark as uploaded
                fileMetadata.MarkAsUploaded();
            }
            else
            {
                // No virus scanning needed - mark as available directly
                fileMetadata.MarkAsAvailable();
            }

            unitOfWork.FileMetadata.Update(fileMetadata);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            // Check if webhook data is incomplete (e.g., S3/SeaweedFS missing ContentType)
            // If so, fetch complete metadata from storage
            var completeMetadata = actualMetadata;
            if (string.IsNullOrWhiteSpace(actualMetadata.ContentType))
            {
                logger.LogInformation(
                    "Webhook metadata incomplete for file {FileId}. Fetching complete metadata from storage.",
                    fileMetadata.Id);

                completeMetadata = await storage.GetMetadataAsync(fileMetadata.StorageKey, cancellationToken);
            }

            // Perform validation by comparing actual metadata with expected metadata (from database)
            var validationErrors = new List<string>();

            // Compare actual file size with expected size
            if (completeMetadata.Size != fileMetadata.Size)
            {
                validationErrors.Add(
                    $"File size mismatch: expected {fileMetadata.Size} bytes, actual {completeMetadata.Size} bytes");
            }

            // Check file size limits
            if (_options.MaxFileSizeBytes > 0 && completeMetadata.Size > _options.MaxFileSizeBytes)
            {
                validationErrors.Add(
                    $"File size {completeMetadata.Size} bytes exceeds maximum allowed size {_options.MaxFileSizeBytes} bytes");
            }

            // Compare actual content type with expected content type
            if (!string.Equals(
                    completeMetadata.ContentType,
                    fileMetadata.ContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add(
                    $"Content type mismatch: expected '{fileMetadata.ContentType}', actual '{completeMetadata.ContentType}'");
            }

            // If there are validation errors, delete from storage and reject
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);

                logger.LogWarning(
                    "File {FileId} validation failed: {Errors}. Deleting from storage.",
                    fileMetadata.Id,
                    errorMessage);

                // Delete file from storage
                try
                {
                    await storage.RemoveAsync(fileMetadata.StorageKey, cancellationToken);

                    logger.LogInformation(
                        "File deleted from storage after validation failure: {StorageKey}",
                        fileMetadata.StorageKey);
                }
                catch (Exception deleteEx)
                {
                    logger.LogError(
                        deleteEx,
                        "Failed to delete file from storage: {StorageKey}",
                        fileMetadata.StorageKey);
                    // Continue with rejection even if deletion fails
                }

                // Reject the file in database
                fileMetadata.Reject(errorMessage);
                unitOfWork.FileMetadata.Update(fileMetadata);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return;
            }

            // Validation passed - determine final status based on virus scanning configuration
            logger.LogInformation(
                "File {FileId} validation passed. Virus scanning enabled: {VirusScanningEnabled}",
                fileMetadata.Id,
                _options.VirusScanningEnabled);

            if (_options.VirusScanningEnabled)
            {
                // Virus scanning is enabled - mark as Uploaded (awaiting scan)
                fileMetadata.MarkAsUploaded();

                logger.LogInformation(
                    "File {FileId} status set to Uploaded (awaiting virus scan)",
                    fileMetadata.Id);
            }
            else
            {
                // Virus scanning is disabled - mark as Available (ready to use)
                fileMetadata.MarkAsAvailable();

                logger.LogInformation(
                    "File {FileId} status set to Available (no virus scanning required)",
                    fileMetadata.Id);
            }

            unitOfWork.FileMetadata.Update(fileMetadata);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error during file validation: {FileId}. Deleting from storage.",
                fileMetadata.Id);

            // Delete file from storage on validation error
            try
            {
                await storage.RemoveAsync(fileMetadata.StorageKey, cancellationToken);
                logger.LogInformation(
                    "File deleted from storage after validation error: {StorageKey}",
                    fileMetadata.StorageKey);
            }
            catch (Exception deleteEx)
            {
                logger.LogError(
                    deleteEx,
                    "Failed to delete file from storage: {StorageKey}",
                    fileMetadata.StorageKey);
                // Continue with rejection even if deletion fails
            }

            fileMetadata.Reject($"Validation error: {ex.Message}");
            unitOfWork.FileMetadata.Update(fileMetadata);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> FileExistsAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if file exists: {FileId}", fileId);

        return await unitOfWork.FileMetadata.AnyAsync(
            f => f.Id == fileId,
            cancellationToken);
    }

    public async Task<bool> FileExistsByHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if file exists by hash: {Hash}", hash);

        return await unitOfWork.FileMetadata.ExistsByHashAsync(hash, cancellationToken);
    }

    private async Task<FileMetadata> GetFileMetadataOrThrowAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var fileMetadata = await unitOfWork.FileMetadata.GetByIdAsync(
            fileId,
            cancellationToken);

        return fileMetadata ?? throw new FileNotFoundException(fileId);
    }
}