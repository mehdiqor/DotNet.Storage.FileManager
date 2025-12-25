namespace FileManager.Application.DTOs;

public sealed record PresignedUploadRequest(
    string Path,
    string FileName,
    string ContentType,
    TimeSpan ExpiresIn,
    long? MaxSize = null
);