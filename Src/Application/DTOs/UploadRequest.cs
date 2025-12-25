namespace FileManager.Application.DTOs;

public sealed record UploadRequest(
    Stream Content,
    string Path,
    string FileName,
    string ContentType,
    long Size,
    string? Hash = null,
    Dictionary<string, string>? Metadata = null
);