using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Raster.Masking;

/// <summary>
/// A raster sequence that applies tick-specific masking to frames.
/// Uses <see cref="ITickBoundRasterMaskFactory"/> to obtain the appropriate mask for each tick.
/// RFC-V2-0028 §3.3.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="MaskedRasterSequence"/> which uses a single static mask,
/// this class requests a fresh mask for each tick from the factory.
/// This enables time-varying masks such as reconstructed plate polygons.
/// </para>
/// </remarks>
public sealed class TickBoundMaskedRasterSequence : IRasterSequence
{
    private readonly IRasterSequence _sourceSequence;
    private readonly ITickBoundRasterMaskFactory _maskFactory;
    private readonly double _noDataValue;

    /// <summary>
    /// Creates a new TickBoundMaskedRasterSequence.
    /// </summary>
    /// <param name="sourceSequence">The source raster sequence to mask.</param>
    /// <param name="maskFactory">Factory that creates tick-appropriate masks.</param>
    /// <param name="noDataValue">Value to use for masked-out pixels.</param>
    public TickBoundMaskedRasterSequence(
        IRasterSequence sourceSequence,
        ITickBoundRasterMaskFactory maskFactory,
        double noDataValue = double.NaN)
    {
        _sourceSequence = sourceSequence ?? throw new ArgumentNullException(nameof(sourceSequence));
        _maskFactory = maskFactory ?? throw new ArgumentNullException(nameof(maskFactory));
        _noDataValue = noDataValue;
    }

    /// <summary>
    /// The underlying (unmasked) raster sequence.
    /// </summary>
    public IRasterSequence SourceSequence => _sourceSequence;

    /// <summary>
    /// The mask factory used to create tick-specific masks.
    /// </summary>
    public ITickBoundRasterMaskFactory MaskFactory => _maskFactory;

    /// <summary>
    /// The no-data value used for masked pixels.
    /// </summary>
    public double NoDataValue => _noDataValue;

    /// <inheritdoc />
    public string SequenceId => $"{_sourceSequence.SequenceId}_tickmasked";

    /// <inheritdoc />
    public string DisplayName => $"{_sourceSequence.DisplayName} (Tick-Masked)";

    /// <inheritdoc />
    public ImmutableArray<CanonicalTick> AvailableTicks => _sourceSequence.AvailableTicks;

    /// <inheritdoc />
    public RasterMetadata Metadata => _sourceSequence.Metadata;

    /// <inheritdoc />
    public IRasterFrame? GetFrameAt(CanonicalTick tick)
    {
        var sourceFrame = _sourceSequence.GetFrameAt(tick);
        if (sourceFrame == null)
            return null;

        var mask = _maskFactory.CreateMask(tick);
        return mask.ApplyMask(sourceFrame, _noDataValue);
    }

    /// <inheritdoc />
    public RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null)
    {
        var sourceResult = _sourceSequence.QueryAt(tick, options);

        if (!sourceResult.HasData)
            return sourceResult;

        var frameData = sourceResult.FrameData!.Value;

        // Get mask for this specific tick
        var mask = _maskFactory.CreateMask(tick);

        // Apply mask to the frame data
        var tempFrame = new SimpleRasterFrame(
            frameData.Width,
            frameData.Height,
            frameData.Bounds,
            frameData.DataType,
            frameData.RawData,
            frameData.NoDataValue);

        var maskedFrame = mask.ApplyMask(tempFrame, _noDataValue);

        var rawData = maskedFrame.GetRawData().ToArray();
        var maskedData = new RasterFrameData(
            maskedFrame.Width,
            maskedFrame.Height,
            maskedFrame.Bounds,
            maskedFrame.DataType,
            maskedFrame.NoDataValue,
            rawData);

        if (sourceResult.IsInterpolated)
        {
            return RasterQueryResult.Interpolated(
                tick,
                sourceResult.SourceFrameTick ?? CanonicalTick.Genesis,
                sourceResult.InterpolationWeight ?? 0,
                maskedData);
        }

        return RasterQueryResult.Exact(tick, maskedData);
    }

    /// <inheritdoc />
    public IEnumerable<IRasterFrame> GetFramesInRange(CanonicalTick startTick, CanonicalTick endTick)
    {
        foreach (var tick in _sourceSequence.AvailableTicks.Where(t => t >= startTick && t <= endTick))
        {
            var frame = GetFrameAt(tick);
            if (frame != null)
                yield return frame;
        }
    }
}
