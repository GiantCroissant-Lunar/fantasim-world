using System.IO;

namespace FantaSim.Geosphere.Plate.Datasets.Import;

internal static class PathResolver
{
    public static bool TryResolveFile(string datasetRootPath, string relativePath, out string absolutePath)
    {
        absolutePath = string.Empty;

        if (string.IsNullOrWhiteSpace(datasetRootPath) || string.IsNullOrWhiteSpace(relativePath))
            return false;

        if (Path.IsPathRooted(relativePath))
            return false;

        var segments = relativePath.Replace('\\', '/').Split('/');
        foreach (var seg in segments)
        {
            if (seg == "..")
                return false;
        }

        var rootFull = Path.GetFullPath(datasetRootPath);
        var candidate = Path.GetFullPath(Path.Combine(rootFull, relativePath));

        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            return false;

        absolutePath = candidate;
        return true;
    }
}
