using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Canonical deterministic ordering for darts and related topology elements.
/// </summary>
/// <remarks>
/// <para>
/// <b>Single Source of Truth</b>: All dart ordering in the codebase MUST use this class.
/// Do not invent ad-hoc sort keys elsewhere — that leads to "why did face IDs change?" bugs.
/// </para>
/// <para>
/// The ordering is designed to be:
/// </para>
/// <list type="bullet">
///   <item><b>Deterministic</b>: Same inputs always produce same order across runs/machines</item>
///   <item><b>Stable</b>: Small floating-point variations don't change order (via AnglePolicy)</item>
///   <item><b>Complete</b>: No ties possible — every distinct dart has a unique position</item>
/// </list>
/// <para>
/// Sort key precedence (first difference wins):
/// </para>
/// <list type="number">
///   <item>Angle (with AnglePolicy for stability)</item>
///   <item>JunctionId (stabilizes vertex-local ordering)</item>
///   <item>BoundaryId</item>
///   <item>SegmentIndex</item>
///   <item>Direction</item>
/// </list>
/// </remarks>
public static class DeterministicOrder
{
    /// <summary>
    /// Compares two darts for canonical ordering.
    /// </summary>
    /// <param name="policy">Angle comparison policy.</param>
    /// <param name="angleA">Angle of dart A in radians (typically from atan2).</param>
    /// <param name="dartA">Dart A.</param>
    /// <param name="junctionIdA">Junction ID where dart A originates (optional stabilizer).</param>
    /// <param name="angleB">Angle of dart B in radians.</param>
    /// <param name="dartB">Dart B.</param>
    /// <param name="junctionIdB">Junction ID where dart B originates.</param>
    /// <returns>
    /// Negative if A &lt; B, positive if A &gt; B, zero only if darts are identical.
    /// </returns>
    public static int CompareDarts(
        AnglePolicy policy,
        double angleA, BoundaryDart dartA, JunctionId junctionIdA,
        double angleB, BoundaryDart dartB, JunctionId junctionIdB)
    {
        // 1) Angle (with policy-based tolerance/quantization)
        var angleCompare = policy.CompareAngles(angleA, angleB);
        if (angleCompare != 0) return angleCompare;

        // 2) Junction (stabilizes vertex-local ordering when angles tie)
        var junctionCompare = junctionIdA.Value.CompareTo(junctionIdB.Value);
        if (junctionCompare != 0) return junctionCompare;

        // 3) Boundary, segment, direction (via BoundaryDart.CompareTo)
        return dartA.CompareTo(dartB);
    }

    /// <summary>
    /// Compares two darts for canonical ordering without junction context.
    /// Use when all darts share the same origin junction.
    /// </summary>
    /// <param name="policy">Angle comparison policy.</param>
    /// <param name="angleA">Angle of dart A in radians.</param>
    /// <param name="dartA">Dart A.</param>
    /// <param name="angleB">Angle of dart B in radians.</param>
    /// <param name="dartB">Dart B.</param>
    /// <returns>
    /// Negative if A &lt; B, positive if A &gt; B, zero only if darts are identical.
    /// </returns>
    public static int CompareDarts(
        AnglePolicy policy,
        double angleA, BoundaryDart dartA,
        double angleB, BoundaryDart dartB)
    {
        // 1) Angle (with policy-based tolerance/quantization)
        var angleCompare = policy.CompareAngles(angleA, angleB);
        if (angleCompare != 0) return angleCompare;

        // 2) Boundary, segment, direction (via BoundaryDart.CompareTo)
        return dartA.CompareTo(dartB);
    }

    /// <summary>
    /// Compares two darts using default angle policy.
    /// Convenience overload for most use cases.
    /// </summary>
    public static int CompareDarts(
        double angleA, BoundaryDart dartA,
        double angleB, BoundaryDart dartB)
        => CompareDarts(AnglePolicy.Default, angleA, dartA, angleB, dartB);

    /// <summary>
    /// Computes the outgoing angle of a direction vector (atan2).
    /// </summary>
    /// <param name="dx">X component of direction.</param>
    /// <param name="dy">Y component of direction.</param>
    /// <returns>Angle in radians, range [-π, π].</returns>
    public static double ComputeAngle(double dx, double dy)
        => Math.Atan2(dy, dx);
}
