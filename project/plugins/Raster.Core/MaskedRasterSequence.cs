using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Masking;

namespace FantaSim.Raster.Core;

/// <summary>
/// A raster sequence that applies masking to frames from a source sequence.
/// RFC-V2-0028 §3.3 compliant.
/// </summary>
/// <remarks>
/// <para>
/// This is the domain-agnostic base implementation. It applies any <see cref="IRasterMask"/>
/// to frames from a source sequence on demand.
/// </para>
/// <para>
/// For plate-specific masking, see <c>Geosphere.Plate.Raster.Masking.Plates</c>.
/// </para>
/// </remarks>
public class MaskedRasterSequence : IMaskedRasterSequence
{
    private readonly IRasterSequence _sourceSequence;
    private readonly IRasterMask _mask;
    private readonly double _noDataValue;

    /// <summary>
    /// Creates a new MaskedRasterSequence.
    /// </summary>
    /// <param name="sourceSequence">The source raster sequence to mask.</param>
    /// <param name="mask">The mask to apply to frames.</param>
    /// <param name="noDataValue">Value to use for masked-out pixels.</param>
    public MaskedRasterSequence(
        IRasterSequence sourceSequence,
        IRasterMask mask,
        double noDataValue)
    {
        _sourceSequence = sourceSequence ?? throw new ArgumentNullException(nameof(sourceSequence));
        _mask = mask ?? throw new ArgumentNullException(nameof(mask));
        _noDataValue = noDataValue;
    }

    /// <inheritdoc />
    public string SequenceId => $"{_sourceSequence.SequenceId}_masked";

    /// <inheritdoc />
    public string DisplayName => $"{_sourceSequence.DisplayName} (Masked)";

    /// <inheritdoc />
    public ImmutableArray<CanonicalTick> AvailableTicks => _sourceSequence.AvailableTicks;

    /// <inheritdoc />
    public RasterMetadata Metadata => _sourceSequence.Metadata;

    /// <inheritdoc />
    public IRasterSequence SourceSequence => _sourceSequence;

    /// <inheritdoc />
    public IRasterMask Mask => _mask;

    /// <summary>
    /// The no-data value used for masked pixels.
    /// </summary>
    protected double NoDataValue => _noDataValue;

    /// <inheritdoc />
    public IRasterFrame? GetFrameAt(CanonicalTick tick)
    {
        var sourceFrame = _sourceSequence.GetFrameAt(tick);
        if (sourceFrame == null)
            return null;

        return _mask.ApplyMask(sourceFrame, _noDataValue);
    }

    /// <inheritdoc />
    public RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null)
    {
        var sourceResult = _sourceSequence.QueryAt(tick, options);

        if (!sourceResult.HasData)
            return sourceResult;

        var frameData = sourceResult.FrameData!.Value;

        // Apply mask to the frame data
        var maskedFrame = _mask.ApplyMask(
            new SimpleRasterFrame(
                frameData.Width,
                frameData.Height,
                frameData.Bounds,
                frameData.DataType,
                frameData.RawData,
                frameData.NoDataValue),
            _noDataValue);

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
        return _sourceSequence
            .GetFramesInRange(startTick, endTick)
            .Select(frame => _mask.ApplyMask(frame, _noDataValue))
            .ToList();
    }
}
