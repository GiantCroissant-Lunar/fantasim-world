using System.Collections.Immutable;
using System.Diagnostics;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Raster.Contracts;

namespace FantaSim.Geosphere.Plate.Raster.GeoTiff;

/// <summary>
/// Implementation of IRasterSequence for GeoTiff data.
/// Supports time-indexed frames from multiple files or multi-band files.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class GeoTiffRasterSequence : IRasterSequence, IDisposable
{
    private readonly SortedDictionary<CanonicalTick, GeoTiffRasterFrame> _frames;
    private readonly object _lock = new();
    private ImmutableArray<CanonicalTick> _availableTicks;

    /// <summary>
    /// Creates a new GeoTiffRasterSequence.
    /// </summary>
    public GeoTiffRasterSequence(
        string sequenceId,
        string displayName,
        IEnumerable<(CanonicalTick tick, GeoTiffRasterFrame frame)> frames,
        RasterMetadata? metadata = null)
    {
        SequenceId = sequenceId;
        DisplayName = displayName;
        _frames = new SortedDictionary<CanonicalTick, GeoTiffRasterFrame>();

        foreach (var (tick, frame) in frames)
        {
            _frames[tick] = frame;
        }

        _availableTicks = _frames.Keys.ToImmutableArray();

        // Derive metadata from first frame if not provided
        if (metadata.HasValue)
        {
            Metadata = metadata.Value;
        }
        else if (_frames.Count > 0)
        {
            var firstFrame = _frames.Values.First();
            Metadata = new RasterMetadata(
                firstFrame.Width,
                firstFrame.Height,
                firstFrame.Bounds,
                firstFrame.DataType,
                firstFrame.NoDataValue,
                null, // CoordinateSystem
                null  // Units
            );
        }
        else
        {
            Metadata = default;
        }
    }

    /// <inheritdoc />
    public string SequenceId { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public ImmutableArray<CanonicalTick> AvailableTicks => _availableTicks;

    /// <inheritdoc />
    public RasterMetadata Metadata { get; }

    /// <inheritdoc />
    public IRasterFrame? GetFrameAt(CanonicalTick tick)
    {
        lock (_lock)
        {
            return _frames.TryGetValue(tick, out var frame) ? frame : null;
        }
    }

    /// <inheritdoc />
    public RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null)
    {
        var opts = options ?? RasterQueryOptions.Default;

        lock (_lock)
        {
            if (_frames.Count == 0)
                return RasterQueryResult.NotFound(tick);

            // Check for exact match first
            if (_frames.TryGetValue(tick, out var exactFrame))
            {
                return RasterQueryResult.Exact(tick, CreateFrameData(exactFrame));
            }

            // Find surrounding frames
            var (beforeTick, beforeFrame) = GetFrameAtOrBefore(tick);
            var (afterTick, afterFrame) = GetFrameAtOrAfter(tick);

            if (beforeFrame == null && afterFrame == null)
                return RasterQueryResult.NotFound(tick);

            // If only one side exists, use it
            if (beforeFrame == null)
            {
                return RasterQueryResult.Exact(tick, CreateFrameData(afterFrame!));
            }
            if (afterFrame == null)
            {
                return RasterQueryResult.Exact(tick, CreateFrameData(beforeFrame));
            }

            // Handle interpolation
            if (opts.Interpolation == InterpolationMethod.NearestNeighbor)
            {
                // Choose the nearest frame
                var beforeDist = tick.Value - beforeTick.Value;
                var afterDist = afterTick.Value - tick.Value;

                if (beforeDist <= afterDist)
                {
                    return RasterQueryResult.Exact(tick, CreateFrameData(beforeFrame));
                }
                else
                {
                    return RasterQueryResult.Exact(tick, CreateFrameData(afterFrame));
                }
            }
            else if (opts.Interpolation == InterpolationMethod.Linear)
            {
                // Linear interpolation between frames
                var totalTicks = afterTick.Value - beforeTick.Value;
                var targetTicks = tick.Value - beforeTick.Value;
                var weight = totalTicks > 0 ? (double)targetTicks / totalTicks : 0;

                var interpolatedData = InterpolateFrames(beforeFrame, afterFrame, weight);
                var frameData = new RasterFrameData(
                    beforeFrame.Width,
                    beforeFrame.Height,
                    beforeFrame.Bounds,
                    beforeFrame.DataType,
                    beforeFrame.NoDataValue,
                    interpolatedData);

                return RasterQueryResult.Interpolated(tick, beforeTick, weight, frameData);
            }

            // Default to before frame
            return RasterQueryResult.Exact(tick, CreateFrameData(beforeFrame));
        }
    }

    /// <inheritdoc />
    public IEnumerable<IRasterFrame> GetFramesInRange(CanonicalTick startTick, CanonicalTick endTick)
    {
        lock (_lock)
        {
            return _frames
                .Where(kvp => kvp.Key >= startTick && kvp.Key <= endTick)
                .Select(kvp => kvp.Value)
                .ToList();
        }
    }

    /// <summary>
    /// Gets the frame at or before the given tick.
    /// </summary>
    private (CanonicalTick tick, GeoTiffRasterFrame? frame) GetFrameAtOrBefore(CanonicalTick tick)
    {
        var result = _frames
            .LastOrDefault(kvp => kvp.Key <= tick);

        return (result.Key, result.Value);
    }

    /// <summary>
    /// Gets the frame at or after the given tick.
    /// </summary>
    private (CanonicalTick tick, GeoTiffRasterFrame? frame) GetFrameAtOrAfter(CanonicalTick tick)
    {
        var result = _frames
            .FirstOrDefault(kvp => kvp.Key >= tick);

        return (result.Key, result.Value);
    }

    /// <summary>
    /// Linearly interpolates between two frames.
    /// </summary>
    private byte[] InterpolateFrames(GeoTiffRasterFrame before, GeoTiffRasterFrame after, double weight)
    {
        Debug.Assert(before.Width == after.Width && before.Height == after.Height);

        var beforeData = before.GetDataArray();
        var afterData = after.GetDataArray();

        Debug.Assert(beforeData.Length == afterData.Length);

        var interpolatedData = new double[beforeData.Length];
        var noDataValue = before.NoDataValue ?? double.NaN;

        for (int i = 0; i < beforeData.Length; i++)
        {
            var beforeVal = beforeData[i];
            var afterVal = afterData[i];

            // Handle no-data values
            var beforeIsNoData = IsNoData(beforeVal, noDataValue);
            var afterIsNoData = IsNoData(afterVal, noDataValue);

            if (beforeIsNoData && afterIsNoData)
            {
                interpolatedData[i] = noDataValue;
            }
            else if (beforeIsNoData)
            {
                interpolatedData[i] = afterVal;
            }
            else if (afterIsNoData)
            {
                interpolatedData[i] = beforeVal;
            }
            else
            {
                interpolatedData[i] = beforeVal + (afterVal - beforeVal) * weight;
            }
        }

        // Convert to bytes
        var bytes = new byte[interpolatedData.Length * sizeof(double)];
        Buffer.BlockCopy(interpolatedData, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static bool IsNoData(double value, double noDataValue)
    {
        if (double.IsNaN(noDataValue))
            return double.IsNaN(value);
        return Math.Abs(value - noDataValue) < 1e-10;
    }

    private static RasterFrameData CreateFrameData(GeoTiffRasterFrame frame)
    {
        return new RasterFrameData(
            frame.Width,
            frame.Height,
            frame.Bounds,
            frame.DataType,
            frame.NoDataValue,
            frame.GetRawData().ToArray());
    }

    /// <summary>
    /// Gets the internal frame for a specific tick (for internal use).
    /// </summary>
    internal GeoTiffRasterFrame? GetInternalFrame(CanonicalTick tick)
    {
        lock (_lock)
        {
            return _frames.TryGetValue(tick, out var frame) ? frame : null;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var frame in _frames.Values)
            {
                frame.Dispose();
            }
            _frames.Clear();
        }
    }
}
