namespace FileManager.Application.DTOs;

public sealed record StorageObjectMetadata(
    string Key,
    long Size,
    string ETag,
    string ContentType,
    DateTime LastModified,
    string? VersionId = null,
    Dictionary<string, string>? Metadata = null
);