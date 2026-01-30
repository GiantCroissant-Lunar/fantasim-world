using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using FantaSim.Raster.Contracts;

namespace FantaSim.Raster.GeoTiff.Internal;

/// <summary>
/// Low-level wrapper for reading GeoTiff files using LibTiff.NET.
/// </summary>
internal sealed class TiffReader : IDisposable
{
    private Tiff? _tiff;
    private bool _disposed;

    // GeoTiff tag constants
    private const TiffTag TIFFTAG_GEOPIXELSCALE = (TiffTag)33550;
    private const TiffTag TIFFTAG_GEOTIEPOINTS = (TiffTag)33922;
    private const TiffTag TIFFTAG_GEOTRANSMATRIX = (TiffTag)34264;
    private const TiffTag TIFFTAG_GEOKEYDIRECTORY = (TiffTag)34735;
    private const TiffTag TIFFTAG_GEODOUBLEPARAMS = (TiffTag)34736;
    private const TiffTag TIFFTAG_GEOASCIIPARAMS = (TiffTag)34737;

    /// <summary>
    /// Opens a Tiff file for reading.
    /// </summary>
    public static TiffReader Open(string path)
    {
        var tiff = Tiff.Open(path, "r");
        if (tiff == null)
        {
            throw new InvalidOperationException($"Failed to open Tiff file: {path}");
        }

        return new TiffReader { _tiff = tiff };
    }

    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public int Width => _tiff?.GetField(TiffTag.IMAGEWIDTH)?[0].ToInt() ?? 0;

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public int Height => _tiff?.GetField(TiffTag.IMAGELENGTH)?[0].ToInt() ?? 0;

    /// <summary>
    /// Gets the number of bands/samples per pixel.
    /// </summary>
    public int SamplesPerPixel => _tiff?.GetField(TiffTag.SAMPLESPERPIXEL)?[0].ToInt() ?? 1;

    /// <summary>
    /// Gets the bits per sample.
    /// </summary>
    public int BitsPerSample
    {
        get
        {
            var field = _tiff?.GetField(TiffTag.BITSPERSAMPLE);
            if (field != null && field.Length > 0)
            {
                return field[0].ToInt();
            }
            return 8;
        }
    }

    /// <summary>
    /// Gets the sample format (unsigned int, signed int, float, etc.).
    /// </summary>
    public SampleFormat SampleFormat
    {
        get
        {
            var field = _tiff?.GetField(TiffTag.SAMPLEFORMAT);
            if (field != null && field.Length > 0)
            {
                return (SampleFormat)field[0].ToInt();
            }
            return SampleFormat.UINT;
        }
    }

    /// <summary>
    /// Gets the planar configuration (chunky or separate).
    /// </summary>
    public PlanarConfig PlanarConfig
    {
        get
        {
            var field = _tiff?.GetField(TiffTag.PLANARCONFIG);
            if (field != null && field.Length > 0)
            {
                return (PlanarConfig)field[0].ToInt();
            }
            return PlanarConfig.CONTIG;
        }
    }

    /// <summary>
    /// Gets the no-data value if specified.
    /// </summary>
    public double? NoDataValue
    {
        get
        {
            var field = _tiff?.GetField(TiffTag.GDAL_NODATA);
            if (field != null && field.Length > 1)
            {
                var value = field[1].ToString();
                if (!string.IsNullOrEmpty(value) && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the geo transformation (pixel to world coordinates).
    /// Returns [originX, pixelWidth, rotationX, originY, rotationY, pixelHeight].
    /// </summary>
    public double[]? GeoTransform
    {
        get
        {
            // Try to get from GeoTransform matrix tag first
            var matrixField = _tiff?.GetField(TIFFTAG_GEOTRANSMATRIX);
            if (matrixField != null && matrixField.Length > 1)
            {
                var count = matrixField[0].ToInt();
                if (count >= 16)
                {
                    // Full 4x4 transformation matrix - extract the 6-element geotransform
                    var bytes = matrixField[1].GetBytes();
                    var matrix = new double[count];
                    Buffer.BlockCopy(bytes, 0, matrix, 0, count * sizeof(double));
                    return new[]
                    {
                        matrix[3],  // originX
                        matrix[0],  // pixelWidth
                        matrix[1],  // rotationX
                        matrix[7],  // originY
                        matrix[4],  // rotationY
                        matrix[5]   // pixelHeight
                    };
                }
            }

            // Try to build from pixel scale and tie points
            var scaleField = _tiff?.GetField(TIFFTAG_GEOPIXELSCALE);
            var tiePointField = _tiff?.GetField(TIFFTAG_GEOTIEPOINTS);

            if (scaleField != null && tiePointField != null && scaleField.Length > 1 && tiePointField.Length > 1)
            {
                var scaleBytes = scaleField[1].GetBytes();
                var scales = new double[3];
                Buffer.BlockCopy(scaleBytes, 0, scales, 0, Math.Min(scaleBytes.Length, 3 * sizeof(double)));

                var tiePointBytes = tiePointField[1].GetBytes();
                var tiePoints = new double[6];
                Buffer.BlockCopy(tiePointBytes, 0, tiePoints, 0, Math.Min(tiePointBytes.Length, 6 * sizeof(double)));

                // tiePoints: [pixelX, pixelY, pixelZ, geoX, geoY, geoZ]
                return new[]
                {
                    tiePoints[3],  // originX
                    scales[0],     // pixelWidth
                    0.0,           // rotationX (no rotation)
                    tiePoints[4],  // originY
                    0.0,           // rotationY (no rotation)
                    -scales[1]     // pixelHeight (negative for top-down)
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the coordinate reference system (CRS) information.
    /// </summary>
    public string? CoordinateReferenceSystem
    {
        get
        {
            // Try to get from GeoAsciiParams
            var asciiParamsField = _tiff?.GetField(TIFFTAG_GEOASCIIPARAMS);
            if (asciiParamsField != null && asciiParamsField.Length > 1)
            {
                var asciiParams = asciiParamsField[1].ToString();
                if (!string.IsNullOrEmpty(asciiParams))
                {
                    // Parse CRS from ASCII params (often contains EPSG code)
                    var epsgMatch = System.Text.RegularExpressions.Regex.Match(
                        asciiParams,
                        @"EPSG\[""?(?<code>\d+)""?\]",
                        System.Text.RegularExpressions.RegexOptions.ExplicitCapture,
                        TimeSpan.FromSeconds(1));
                    if (epsgMatch.Success)
                    {
                        return $"EPSG:{epsgMatch.Groups["code"].Value}";
                    }
                }
            }

            // Try to parse from GeoKeyDirectory
            var keyDirField = _tiff?.GetField(TIFFTAG_GEOKEYDIRECTORY);
            if (keyDirField != null && keyDirField.Length > 1)
            {
                var keyBytes = keyDirField[1].GetBytes();
                var keyCount = keyDirField[0].ToInt();
                var keys = new short[keyCount];
                Buffer.BlockCopy(keyBytes, 0, keys, 0, Math.Min(keyBytes.Length, keyCount * sizeof(short)));

                // Look for GeographicTypeGeoKey (1024) or ProjectedCSTypeGeoKey (3072)
                for (int i = 4; i < keys.Length; i += 4)
                {
                    var keyId = keys[i];
                    var value = keys[i + 3];

                    if (keyId == 1024 && value != 32767) // GeographicTypeGeoKey
                    {
                        return $"EPSG:{value}";
                    }
                    if (keyId == 3072 && value != 32767) // ProjectedCSTypeGeoKey
                    {
                        return $"EPSG:{value}";
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Maps the Tiff sample format and bits per sample to RasterDataType.
    /// </summary>
    public RasterDataType GetRasterDataType()
    {
        var sampleFormat = SampleFormat;
        var bitsPerSample = BitsPerSample;

        return sampleFormat switch
        {
            SampleFormat.UINT => bitsPerSample switch
            {
                8 => RasterDataType.UInt8,
                16 => RasterDataType.UInt16,
                32 => RasterDataType.UInt32,
                _ => RasterDataType.UInt16
            },
            SampleFormat.INT => bitsPerSample switch
            {
                8 => RasterDataType.Int8,
                16 => RasterDataType.Int16,
                32 => RasterDataType.Int32,
                _ => RasterDataType.Int16
            },
            SampleFormat.IEEEFP => bitsPerSample switch
            {
                32 => RasterDataType.Float32,
                64 => RasterDataType.Float64,
                _ => RasterDataType.Float32
            },
            _ => RasterDataType.Float32
        };
    }

    /// <summary>
    /// Reads a single band into a typed array.
    /// </summary>
    public T[] ReadBand<T>(int bandIndex = 0) where T : unmanaged
    {
        if (_tiff == null)
            throw new ObjectDisposedException(nameof(TiffReader));

        var width = Width;
        var height = Height;
        var samplesPerPixel = SamplesPerPixel;

        if (bandIndex < 0 || bandIndex >= samplesPerPixel)
            throw new ArgumentOutOfRangeException(nameof(bandIndex));

        var result = new T[width * height];
        var scanlineSize = _tiff.ScanlineSize();
        var buffer = new byte[scanlineSize];

        var gcHandle = GCHandle.Alloc(result, GCHandleType.Pinned);
        try
        {
            var resultPtr = gcHandle.AddrOfPinnedObject();

            for (int row = 0; row < height; row++)
            {
                _tiff.ReadScanline(buffer, row);

                // Copy scanline to result array
                var rowOffset = row * width;
                var elementSize = Marshal.SizeOf<T>();

                for (int col = 0; col < width; col++)
                {
                    var pixelOffset = (col * samplesPerPixel + bandIndex) * elementSize;
                    var resultOffset = (rowOffset + col) * elementSize;

                    if (pixelOffset + elementSize <= buffer.Length)
                    {
                        Marshal.Copy(buffer, pixelOffset, resultPtr + resultOffset, elementSize);
                    }
                }
            }
        }
        finally
        {
            gcHandle.Free();
        }

        return result;
    }

    /// <summary>
    /// Reads a single band as double values.
    /// </summary>
    public double[] ReadBandAsDouble(int bandIndex = 0)
    {
        var dataType = GetRasterDataType();

        return dataType switch
        {
            RasterDataType.UInt8 => ReadBand<byte>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.Int8 => ReadBand<sbyte>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.UInt16 => ReadBand<ushort>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.Int16 => ReadBand<short>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.UInt32 => ReadBand<uint>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.Int32 => ReadBand<int>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.Float32 => ReadBand<float>(bandIndex).Select(b => (double)b).ToArray(),
            RasterDataType.Float64 => ReadBand<double>(bandIndex),
            _ => ReadBand<float>(bandIndex).Select(b => (double)b).ToArray()
        };
    }

    /// <summary>
    /// Gets all band indices available in this file.
    /// </summary>
    public IEnumerable<int> GetBandIndices()
    {
        var count = SamplesPerPixel;
        for (int i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tiff?.Dispose();
            _tiff = null;
            _disposed = true;
        }
    }
}
