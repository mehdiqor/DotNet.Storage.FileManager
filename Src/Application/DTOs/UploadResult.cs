namespace FileManager.Application.DTOs;

public sealed record UploadResult(
    string StorageKey,
    string ETag,
    long Size,
    string? VersionId = null
);