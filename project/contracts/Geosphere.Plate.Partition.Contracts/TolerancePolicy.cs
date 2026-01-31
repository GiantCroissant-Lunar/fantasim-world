using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Base record for tolerance policies in plate partition operations.
/// Controls geometric tolerance thresholds for the partition algorithm.
/// RFC-V2-0047 §6.1.
/// </summary>
[MessagePackObject]
[Union(0, typeof(StrictPolicy))]
[Union(1, typeof(LenientPolicy))]
[Union(2, typeof(PolygonizerDefaultPolicy))]
[StructLayout(LayoutKind.Auto)]
public abstract record TolerancePolicy
{
    private TolerancePolicy() { }

    /// <summary>
    /// Strict policy with zero tolerance. No geometric errors are permitted.
    /// </summary>
    [MessagePackObject]
    public sealed record StrictPolicy : TolerancePolicy
    {
        public StrictPolicy() { }
    }

    /// <summary>
    /// Lenient policy with configurable epsilon tolerance (in radians).
    /// </summary>
    /// <param name="Epsilon">Tolerance threshold in radians for geometric comparisons.</param>
    [MessagePackObject]
    public sealed record LenientPolicy(
        [property: Key(0)] double Epsilon
    ) : TolerancePolicy
    {
        public LenientPolicy() : this(1e-9) { }
    }

    /// <summary>
    /// Default policy that auto-selects appropriate tolerance based on context.
    /// </summary>
    [MessagePackObject]
    public sealed record PolygonizerDefaultPolicy : TolerancePolicy
    {
        public PolygonizerDefaultPolicy() { }
    }
}
