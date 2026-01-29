using System.Text.RegularExpressions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Raster.GeoTiff.Internal;

namespace FantaSim.Geosphere.Plate.Raster.GeoTiff;

/// <summary>
/// Loads raster sequences from GeoTiff files.
/// Supports single files, multi-band files, and file pattern sequences.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class GeoTiffRasterLoader : IRasterSequenceLoader
{
    private const string FormatName = "geotiff";

    /// <summary>
    /// File extensions supported by this loader.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedExtensions = new[] { ".tif", ".tiff", ".geotiff" };

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedFormats => new[] { FormatName, "tiff", "tif" };

    /// <inheritdoc />
    public bool CanLoad(string format)
    {
        return SupportedFormats.Contains(format.ToLowerInvariant());
    }

    /// <inheritdoc />
    public async Task<IRasterSequence> LoadAsync(
        RasterSequenceAsset asset,
        IPlatesDataset dataset,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(dataset.DatasetRootPath, asset.RelativePath);

        // Determine loading strategy based on path
        if (File.Exists(fullPath))
        {
            return await LoadSingleFileAsync(asset, fullPath, cancellationToken);
        }

        // Check if it's a pattern (contains wildcards)
        if (fullPath.Contains('*') || fullPath.Contains('?'))
        {
            return await LoadPatternAsync(asset, fullPath, cancellationToken);
        }

        // Check if it's a directory
        if (Directory.Exists(fullPath))
        {
            return await LoadDirectoryAsync(asset, fullPath, cancellationToken);
        }

        throw new FileNotFoundException($"GeoTiff source not found: {fullPath}");
    }

    /// <summary>
    /// Loads a single GeoTiff file as a sequence.
    /// Multi-band files are treated as multiple frames.
    /// </summary>
    private async Task<IRasterSequence> LoadSingleFileAsync(
        RasterSequenceAsset asset,
        string filePath,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            using var reader = TiffReader.Open(filePath);

            var geoTransform = reader.GeoTransform ?? DefaultGeoTransform();
            var bounds = CalculateBounds(reader.Width, reader.Height, geoTransform);
            var dataType = reader.GetRasterDataType();
            var noDataValue = reader.NoDataValue;

            var frames = new List<(CanonicalTick, GeoTiffRasterFrame)>();
            var bandCount = reader.SamplesPerPixel;
            var baseTick = CanonicalTick.Genesis;

            if (bandCount == 1)
            {
                // Single band - single frame
                var data = reader.ReadBandAsDouble(0);
                var frame = new GeoTiffRasterFrame(
                    baseTick,
                    reader.Width,
                    reader.Height,
                    bounds,
                    dataType,
                    data,
                    noDataValue);

                frames.Add((baseTick, frame));
            }
            else
            {
                // Multi-band - each band is a frame
                for (int bandIndex = 0; bandIndex < bandCount; bandIndex++)
                {
                    ct.ThrowIfCancellationRequested();

                    var tick = new CanonicalTick(baseTick.Value + bandIndex);

                    var data = reader.ReadBandAsDouble(bandIndex);
                    var frame = new GeoTiffRasterFrame(
                        tick,
                        reader.Width,
                        reader.Height,
                        bounds,
                        dataType,
                        data,
                        noDataValue);

                    frames.Add((tick, frame));
                }
            }

            return new GeoTiffRasterSequence(
                asset.AssetId,
                Path.GetFileNameWithoutExtension(filePath),
                frames);
        }, ct);
    }

    /// <summary>
    /// Loads multiple files matching a pattern as a sequence.
    /// </summary>
    private async Task<IRasterSequence> LoadPatternAsync(
        RasterSequenceAsset asset,
        string pattern,
        CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(pattern) ?? ".";
        var filePattern = Path.GetFileName(pattern);

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Directory not found: {dir}");

        var files = Directory.GetFiles(dir, filePattern)
            .Where(f => SupportedExtensions.Any(e =>
                f.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
            throw new FileNotFoundException($"No GeoTiff files match pattern: {pattern}");

        return await LoadFilesAsync(asset, files, ct);
    }

    /// <summary>
    /// Loads all GeoTiff files in a directory as a sequence.
    /// </summary>
    private async Task<IRasterSequence> LoadDirectoryAsync(
        RasterSequenceAsset asset,
        string directoryPath,
        CancellationToken ct)
    {
        var files = Directory.GetFiles(directoryPath)
            .Where(f => SupportedExtensions.Any(e =>
                f.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
            throw new FileNotFoundException($"No GeoTiff files in directory: {directoryPath}");

        return await LoadFilesAsync(asset, files, ct);
    }

    /// <summary>
    /// Loads a list of files as a sequence, extracting tick information from filenames if possible.
    /// </summary>
    private async Task<IRasterSequence> LoadFilesAsync(
        RasterSequenceAsset asset,
        List<string> files,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var frames = new List<(CanonicalTick, GeoTiffRasterFrame)>();
            var baseTick = CanonicalTick.Genesis;

            foreach (var (file, index) in files.Select((f, i) => (f, i)))
            {
                ct.ThrowIfCancellationRequested();

                using var reader = TiffReader.Open(file);

                var geoTransform = reader.GeoTransform ?? DefaultGeoTransform();
                var bounds = CalculateBounds(reader.Width, reader.Height, geoTransform);
                var dataType = reader.GetRasterDataType();
                var noDataValue = reader.NoDataValue;

                // Determine tick for this file
                var tick = ExtractTickFromFilename(file, baseTick, index);

                var data = reader.ReadBandAsDouble(0);
                var frame = new GeoTiffRasterFrame(
                    tick,
                    reader.Width,
                    reader.Height,
                    bounds,
                    dataType,
                    data,
                    noDataValue);

                frames.Add((tick, frame));
            }

            return new GeoTiffRasterSequence(
                asset.AssetId,
                asset.AssetId,
                frames);
        }, ct);
    }

    /// <summary>
    /// Attempts to extract a tick value from a filename.
    /// Supports patterns like "elevation_100.tif", "data_001.tif", etc.
    /// </summary>
    private static CanonicalTick ExtractTickFromFilename(
        string filePath,
        CanonicalTick baseTick,
        int index)
    {
        var filename = Path.GetFileNameWithoutExtension(filePath);

        // Try to find numeric suffix/pattern
        // Pattern: any sequence of digits at the end or preceded by underscore/hyphen
        var match = Regex.Match(filename, @"[_\-]?(?<num>\d+)$", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

        if (match.Success)
        {
            var numberStr = match.Groups["num"].Value;
            if (long.TryParse(numberStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var tickValue))
            {
                return new CanonicalTick(tickValue);
            }
        }

        // Try to find tick pattern with prefix like "tick123" or "t_456"
        match = Regex.Match(filename, @"[Tt]ick[_\-]?(?<num>\d+)", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
        if (match.Success)
        {
            var numberStr = match.Groups["num"].Value;
            if (long.TryParse(numberStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var tickValue))
            {
                return new CanonicalTick(tickValue);
            }
        }

        // Default: calculate tick based on index
        return new CanonicalTick(baseTick.Value + index);
    }

    /// <summary>
    /// Returns a default geo transform for files without geo-referencing.
    /// </summary>
    private static double[] DefaultGeoTransform()
    {
        return new[] { 0.0, 1.0, 0.0, 0.0, 0.0, -1.0 };
    }

    /// <summary>
    /// Calculates geographic bounds from geo transformation.
    /// </summary>
    public static RasterBounds CalculateBounds(
        int width,
        int height,
        double[] geoTransform)
    {
        // geoTransform: [originX, pixelWidth, rotationX, originY, rotationY, pixelHeight]
        var originX = geoTransform[0];
        var pixelWidth = geoTransform[1];
        var rotationX = geoTransform[2];
        var originY = geoTransform[3];
        var rotationY = geoTransform[4];
        var pixelHeight = geoTransform[5];

        // For unrotated rasters (rotationX == 0, rotationY == 0)
        if (Math.Abs(rotationX) < 1e-10 && Math.Abs(rotationY) < 1e-10)
        {
            var minLon = originX;
            var maxLon = originX + width * pixelWidth;
            var maxLat = originY;
            var minLat = originY + height * pixelHeight; // pixelHeight is typically negative

            // Ensure proper ordering
            if (minLon > maxLon)
                (minLon, maxLon) = (maxLon, minLon);
            if (minLat > maxLat)
                (minLat, maxLat) = (maxLat, minLat);

            return new RasterBounds(minLon, maxLon, minLat, maxLat);
        }

        // For rotated rasters, calculate the bounding box of all corners
        var corners = new (double x, double y)[4];
        corners[0] = TransformPixelToGeo(0, 0, geoTransform);
        corners[1] = TransformPixelToGeo(width, 0, geoTransform);
        corners[2] = TransformPixelToGeo(width, height, geoTransform);
        corners[3] = TransformPixelToGeo(0, height, geoTransform);

        var cornerMinLon = corners.Min(c => c.x);
        var cornerMaxLon = corners.Max(c => c.x);
        var cornerMinLat = corners.Min(c => c.y);
        var cornerMaxLat = corners.Max(c => c.y);

        return new RasterBounds(cornerMinLon, cornerMaxLon, cornerMinLat, cornerMaxLat);
    }

    /// <summary>
    /// Transforms pixel coordinates to geographic coordinates.
    /// </summary>
    private static (double x, double y) TransformPixelToGeo(
        double pixelX,
        double pixelY,
        double[] geoTransform)
    {
        var originX = geoTransform[0];
        var pixelWidth = geoTransform[1];
        var rotationX = geoTransform[2];
        var originY = geoTransform[3];
        var rotationY = geoTransform[4];
        var pixelHeight = geoTransform[5];

        var geoX = originX + pixelX * pixelWidth + pixelY * rotationX;
        var geoY = originY + pixelX * rotationY + pixelY * pixelHeight;

        return (geoX, geoY);
    }
}
