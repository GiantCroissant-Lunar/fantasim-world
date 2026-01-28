using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Contracts;

/// <summary>
/// Time-dependent raster sequence queryable by tick.
/// RFC-V2-0028 §2.
/// </summary>
public interface IRasterSequence
{
    /// <summary>
    /// Unique identifier for this raster sequence.
    /// </summary>
    string SequenceId { get; }
    
    /// <summary>
    /// Human-readable name/description.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// All available ticks in this sequence (sorted ascending).
    /// </summary>
    ImmutableArray<CanonicalTick> AvailableTicks { get; }
    
    /// <summary>
    /// Minimum tick available.
    /// </summary>
    CanonicalTick MinTick => AvailableTicks.IsDefaultOrEmpty ? CanonicalTick.MinValue : AvailableTicks[0];
    
    /// <summary>
    /// Maximum tick available.
    /// </summary>
    CanonicalTick MaxTick => AvailableTicks.IsDefaultOrEmpty ? CanonicalTick.MaxValue : AvailableTicks[AvailableTicks.Length - 1];
    
    /// <summary>
    /// Gets the raster frame at a specific tick.
    /// Returns null if no frame exists at exactly this tick.
    /// </summary>
    /// <param name="tick">The target tick.</param>
    IRasterFrame? GetFrameAt(CanonicalTick tick);
    
    /// <summary>
    /// Queries the sequence for a raster at the target tick.
    /// Per RFC-V2-0028 §3.1, selects the nearest frame at or before the target tick
    /// when interpolation is disabled.
    /// </summary>
    /// <param name="tick">The target tick.</param>
    /// <param name="options">Query options (interpolation, etc.).</param>
    RasterQueryResult QueryAt(CanonicalTick tick, RasterQueryOptions? options = null);
    
    /// <summary>
    /// Gets all frames in the specified tick range (inclusive).
    /// </summary>
    /// <param name="startTick">Start tick (inclusive).</param>
    /// <param name="endTick">End tick (inclusive).</param>
    IEnumerable<IRasterFrame> GetFramesInRange(CanonicalTick startTick, CanonicalTick endTick);
    
    /// <summary>
    /// Common raster metadata (consistent across all frames).
    /// </summary>
    RasterMetadata Metadata { get; }
}
