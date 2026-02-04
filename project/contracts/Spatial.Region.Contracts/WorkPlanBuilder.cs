namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Utilities for decomposing a <see cref="RegionSpec"/> into a <see cref="WorkPlan"/>.
/// See RFC-V2-0055a §4.1–§4.4.
/// </summary>
public static class WorkPlanBuilder
{
    /// <summary>
    /// Options that influence WorkPlan decomposition.
    /// Per RFC-V2-0055a §4.2–§4.3 these are generator choices, not truth commitments.
    /// </summary>
    /// <param name="TargetChunkSize">
    /// Advisory target size for chunks (implementation-defined units).
    /// Chunking strategy is generator-specific; this is a hint for future decomposers.
    /// </param>
    /// <param name="DefaultHaloM">
    /// Default buffer halo width in meters.
    /// Per RFC-V2-0055a §4.3, halo data may be loaded but MUST NOT be included in output.
    /// </param>
    public sealed record WorkPlanOptions(
        int TargetChunkSize = 1,
        double DefaultHaloM = 0.0);

    /// <summary>
    /// Build a work plan for the provided region.
    /// Per RFC-V2-0055a §4.2, the plan is an ordered set of non-overlapping chunks
    /// suitable for per-chunk computation and deterministic merge.
    /// </summary>
    /// <remarks>
    /// Current implementation returns a single chunk covering the full region.
    /// This preserves deterministic merge semantics (RFC-V2-0055a §4.4) and provides
    /// a stable contract for later decomposition strategies.
    /// </remarks>
    public static WorkPlan BuildPlan(RegionSpec region, WorkPlanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(region);

        options ??= new WorkPlanOptions();

        return new WorkPlan
        {
            Region = region,
            Chunks =
            [
                new WorkChunk
                {
                    Index = 0,
                    Extent = region,
                    HaloM = options.DefaultHaloM
                }
            ]
        };
    }
}
