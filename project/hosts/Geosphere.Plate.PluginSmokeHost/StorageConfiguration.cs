namespace FantaSim.Geosphere.Plate.PluginSmokeHost;

/// <summary>
/// Configuration helper for setting up ZoneTree storage with disk persistence.
/// </summary>
internal static class StorageConfiguration
{
    private const string DefaultStorageDirName = "data";
    private const string DefaultZoneTreeDirName = "zonetree";
    private const string StoragePathEnvVar = "FANTASIM_STORAGE_PATH";

    /// <summary>
    /// Gets the storage path from environment variable or uses default.
    /// </summary>
    public static string GetStoragePath()
    {
        var envPath = Environment.GetEnvironmentVariable(StoragePathEnvVar);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, DefaultStorageDirName, DefaultZoneTreeDirName);
    }

    /// <summary>
    /// Creates a new ZoneTreeKeyValueStore with disk persistence.
    /// </summary>
    public static ZoneTreeKeyValueStore CreateStore(string? customPath = null)
    {
        var path = customPath ?? GetStoragePath();
        return new ZoneTreeKeyValueStore(path);
    }

    /// <summary>
    /// Clears the storage directory if it exists.
    /// Call this on startup for a fresh state each run.
    /// </summary>
    public static void ClearStorage(string? customPath = null)
    {
        var path = customPath ?? GetStoragePath();
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                // Log but don't fail - storage might be in use or permissions issue
                Console.WriteLine($"Warning: Could not clear storage directory '{path}': {ex.Message}");
            }
        }
    }
}
