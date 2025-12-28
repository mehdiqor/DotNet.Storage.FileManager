namespace FileManager.Common.Utilities;

/// <summary>
/// Utility class for generating consistent storage keys across all storage providers.
/// </summary>
public static class StorageKeyGenerator
{
    /// <summary>
    /// Builds a storage key from a path and filename.
    /// </summary>
    /// <param name="path">The path (can include leading/trailing slashes).</param>
    /// <param name="fileName">The filename to append.</param>
    /// <returns>A normalized storage key in the format "path/fileName" or just "fileName" if path is empty.</returns>
    public static string Build(string path, string fileName)
    {
        var cleanPath = path.Trim('/');
        return string.IsNullOrWhiteSpace(cleanPath)
            ? fileName
            : $"{cleanPath}/{fileName}";
    }
}