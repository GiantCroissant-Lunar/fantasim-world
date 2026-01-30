using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Export;
using BitMiracle.LibTiff.Classic;

namespace FantaSim.Raster.GeoTiff;

/// <summary>
/// Exports raster sequences to GeoTiff format.
/// Supports single-frame and multi-frame (time-series) exports.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class GeoTiffRasterExporter : IRasterSequenceExporter
{
    // GeoTiff tag constants (not in BitMiracle.LibTiff.Classic.TiffTag)
    private const TiffTag TIFFTAG_GEOPIXELSCALE = (TiffTag)33550;
    private const TiffTag TIFFTAG_GEOTIEPOINTS = (TiffTag)33922;
    private const TiffTag TIFFTAG_GEOKEYDIRECTORY = (TiffTag)34735;

    // Static flag to ensure tag extender is registered only once
    private static bool _geoTiffTagsRegistered;
    private static readonly object _registrationLock = new();

    /// <summary>
    /// Supported export formats.
    /// </summary>
    public static readonly IReadOnlyCollection<RasterExportFormat> SupportedFormatsList = new[]
    {
        RasterExportFormat.GeoTiff
    };

    /// <inheritdoc />
    public IReadOnlyCollection<RasterExportFormat> SupportedFormats => SupportedFormatsList;

    /// <summary>
    /// Static constructor to register GeoTiff tags with LibTiff.NET.
    /// </summary>
    static GeoTiffRasterExporter()
    {
        EnsureGeoTiffTagsRegistered();
    }

    /// <summary>
    /// Ensures GeoTiff custom tags are registered with LibTiff.NET.
    /// This is required for writing GeoTiff-specific tags.
    /// </summary>
    private static void EnsureGeoTiffTagsRegistered()
    {
        if (_geoTiffTagsRegistered)
            return;

        lock (_registrationLock)
        {
            if (_geoTiffTagsRegistered)
                return;

            // Get the parent tag extender (if any)
            var parentExtender = Tiff.SetTagExtender(GeoTiffTagExtender);

            // If there was a parent, we need to chain to it
            if (parentExtender != null)
            {
                _parentTagExtender = parentExtender;
            }

            _geoTiffTagsRegistered = true;
        }
    }

    private static Tiff.TiffExtendProc? _parentTagExtender;

    /// <summary>
    /// Tag extender that adds GeoTiff tag definitions.
    /// </summary>
    private static void GeoTiffTagExtender(Tiff tiff)
    {
        // Call parent extender first if present
        _parentTagExtender?.Invoke(tiff);

        // Register GeoTiff tags
        var geoTiffTags = new TiffFieldInfo[]
        {
            new(TIFFTAG_GEOPIXELSCALE, TiffFieldInfo.Variable, TiffFieldInfo.Variable,
                TiffType.DOUBLE, FieldBit.Custom, true, true, "GeoPixelScale"),
            new(TIFFTAG_GEOTIEPOINTS, TiffFieldInfo.Variable, TiffFieldInfo.Variable,
                TiffType.DOUBLE, FieldBit.Custom, true, true, "GeoTiePoints"),
            new(TIFFTAG_GEOKEYDIRECTORY, TiffFieldInfo.Variable, TiffFieldInfo.Variable,
                TiffType.SHORT, FieldBit.Custom, true, true, "GeoKeyDirectory"),
        };

        tiff.MergeFieldInfo(geoTiffTags, geoTiffTags.Length);
    }

    /// <inheritdoc />
    public async Task<RasterExportResult> ExportAsync(
        IRasterSequence sequence,
        RasterExportSpec spec,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sequence);

        // Validate spec
        if (!spec.IsValid(out var validationError))
        {
            return RasterExportResult.Failed(RasterExportError.InvalidSpec(validationError ?? "Invalid specification"));
        }

        var outputFiles = new List<string>();
        var errors = new List<RasterExportError>();
        var framesExported = 0;

        try
        {
            // Ensure output directory exists
            if (!Directory.Exists(spec.OutputDirectory))
            {
                Directory.CreateDirectory(spec.OutputDirectory);
            }

            // Get ticks to export using the spec's built-in method
            var ticksToExport = spec.GetExportTicks().ToList();

            if (ticksToExport.Count == 0)
            {
                errors.Add(RasterExportError.InvalidSpec("No frames to export in the specified tick range"));
                return RasterExportResult.Failed(errors.ToArray());
            }

            // Get CRS from sequence metadata (planet-agnostic: may be null)
            var coordinateSystem = sequence.Metadata.CoordinateSystem;

            // Export each frame
            foreach (var tick in ticksToExport)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var frame = sequence.GetFrameAt(tick);
                if (frame == null)
                {
                    errors.Add(RasterExportError.MissingFrame($"Frame not found at tick {tick.Value}"));
                    continue;
                }

                var fileName = spec.GetOutputFileName(tick);
                var filePath = Path.Combine(spec.OutputDirectory, fileName);

                // Ensure .tif extension
                if (!filePath.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) &&
                    !filePath.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".tif";
                }

                var success = await ExportFrameAsync(frame, filePath, coordinateSystem, cancellationToken).ConfigureAwait(false);

                if (success)
                {
                    outputFiles.Add(filePath);
                    framesExported++;
                }
                else
                {
                    errors.Add(RasterExportError.FormatError($"Failed to export frame at tick {tick.Value}", filePath));
                }
            }

            return new RasterExportResult(
                Success: errors.Count == 0,
                OutputFiles: outputFiles,
                Errors: errors,
                FramesExported: framesExported);
        }
        catch (OperationCanceledException)
        {
            return new RasterExportResult(
                Success: false,
                OutputFiles: outputFiles,
                Errors: errors,
                FramesExported: framesExported);
        }
        catch (Exception ex)
        {
            errors.Add(RasterExportError.IOError($"Export failed: {ex.Message}"));
            return RasterExportResult.Failed(errors.ToArray());
        }
    }

    /// <summary>
    /// Exports a single frame to GeoTiff.
    /// </summary>
    private static async Task<bool> ExportFrameAsync(
        IRasterFrame frame,
        string filePath,
        string? coordinateSystem,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create GeoTiff file
                using var tiff = Tiff.Open(filePath, "w");
                if (tiff == null)
                    return false;

                // Set basic tags
                tiff.SetField(TiffTag.IMAGEWIDTH, frame.Width);
                tiff.SetField(TiffTag.IMAGELENGTH, frame.Height);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                tiff.SetField(TiffTag.BITSPERSAMPLE, 64);
                tiff.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                tiff.SetField(TiffTag.ROWSPERSTRIP, 1);
                tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                // Use LZW compression by default
                tiff.SetField(TiffTag.COMPRESSION, Compression.LZW);

                // Set geotiff tags (planet-agnostic: only writes CRS if known)
                SetGeoTiffTags(tiff, frame, coordinateSystem);

                // Write data row by row
                var rawData = frame.GetRawData();
                var stride = frame.Width * sizeof(double);

                for (int row = 0; row < frame.Height; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowData = rawData.Slice(row * stride, stride).ToArray();

                    if (!tiff.WriteScanline(rowData, row))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets GeoTiff georeferencing tags.
    /// Planet-agnostic: writes CRS only if explicitly provided in metadata.
    /// Always writes geotransform (pixel scale + tie points) for coordinate mapping.
    /// </summary>
    private static void SetGeoTiffTags(Tiff tiff, IRasterFrame frame, string? coordinateSystem)
    {
        var bounds = frame.Bounds;

        // Calculate pixel dimensions
        var pixelWidth = bounds.Width / frame.Width;
        var pixelHeight = bounds.Height / frame.Height;

        // Always set GeoPixelScale (X, Y, Z scales) - needed for pixel-to-coordinate mapping
        tiff.SetField(TIFFTAG_GEOPIXELSCALE, 3, new double[] { pixelWidth, pixelHeight, 0.0 });

        // Always set GeoTiePoints (raster point -> model point mapping)
        // Format: I, J, K, X, Y, Z (raster col, row, 0, lon, lat, 0)
        tiff.SetField(TIFFTAG_GEOTIEPOINTS, 6, new double[]
        {
            0.0, 0.0, 0.0,                              // Raster point (0,0)
            bounds.MinLongitude, bounds.MaxLatitude, 0.0  // Model point (top-left corner)
        });

        // Only set GeoKeyDirectory if we have a known EPSG code
        // Planet-agnostic: do NOT assume Earth/WGS84 if CRS is unknown
        var epsgCode = TryParseEpsgCode(coordinateSystem);
        if (epsgCode.HasValue)
        {
            // Format: KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys,
            //         then for each key: KeyID, TIFFTagLocation, Count, Value
            tiff.SetField(TIFFTAG_GEOKEYDIRECTORY, 16, new short[]
            {
                1, 1, 0, 3,                          // Version 1.1.0, 3 keys
                1024, 0, 1, 2,                       // GTModelTypeGeoKey = Geographic
                1025, 0, 1, 1,                       // GTRasterTypeGeoKey = RasterPixelIsArea
                2048, 0, 1, (short)epsgCode.Value    // GeographicTypeGeoKey = provided EPSG
            });
        }
        // If no EPSG code: omit GeoKeyDirectory entirely (CRS-agnostic output)
        // The geotransform (PixelScale + TiePoints) still enables coordinate mapping
    }

    /// <summary>
    /// Tries to parse an EPSG code from a coordinate system string.
    /// Supports formats: "EPSG:4326", "epsg:4326", "4326"
    /// </summary>
    private static int? TryParseEpsgCode(string? coordinateSystem)
    {
        if (string.IsNullOrWhiteSpace(coordinateSystem))
            return null;

        var trimmed = coordinateSystem.Trim();

        // Try "EPSG:XXXX" format (case-insensitive)
        if (trimmed.StartsWith("EPSG:", StringComparison.OrdinalIgnoreCase))
        {
            var codeStr = trimmed.Substring(5);
            if (int.TryParse(codeStr, out var code) && code > 0 && code <= 32767)
                return code;
        }

        // Try raw integer format
        if (int.TryParse(trimmed, out var rawCode) && rawCode > 0 && rawCode <= 32767)
            return rawCode;

        return null;
    }
}
