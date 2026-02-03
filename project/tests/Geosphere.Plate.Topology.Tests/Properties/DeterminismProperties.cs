using FsCheck.Xunit;
using FsCheck;
using Plate.TimeDete.Determinism.Pcg;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Tests.Arbitraries;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Properties;

/// <summary>
/// Property-based tests for ID generation determinism per RFC-V2-0001.
/// </summary>
public class DeterminismProperties
{
    public DeterminismProperties()
    {
        // Register custom arbitraries
        Arb.Register<IdentityArbitrariesRegistration>();
    }

    /// <summary>
    /// Property: Same seed must produce identical PlateId sequences.
    /// This is the core determinism guarantee for replayability.
    /// </summary>
    [Property]
    public Property SameSeedProducesIdenticalPlateIdSequences(ulong seed, int count)
    {
        // Constrain count to reasonable range for performance
        var constrainedCount = Math.Max(1, Math.Min(count, 100));

        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        var ids1 = Enumerable.Range(0, constrainedCount).Select(_ => PlateId.NewId(rng1));
        var ids2 = Enumerable.Range(0, constrainedCount).Select(_ => PlateId.NewId(rng2));

        return ids1.SequenceEqual(ids2).ToProperty();
    }

    /// <summary>
    /// Property: Same seed must produce identical BoundaryId sequences.
    /// </summary>
    [Property]
    public Property SameSeedProducesIdenticalBoundaryIdSequences(ulong seed, int count)
    {
        var constrainedCount = Math.Max(1, Math.Min(count, 100));

        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        var ids1 = Enumerable.Range(0, constrainedCount).Select(_ => BoundaryId.NewId(rng1));
        var ids2 = Enumerable.Range(0, constrainedCount).Select(_ => BoundaryId.NewId(rng2));

        return ids1.SequenceEqual(ids2).ToProperty();
    }

    /// <summary>
    /// Property: PlateId round-trip through string representation preserves identity.
    /// </summary>
    [Property]
    public Property PlateIdRoundTripPreservation(PlateId original)
    {
        var str = original.ToString();
        var parsed = PlateId.Parse(str);
        return (original == parsed).ToProperty();
    }

    /// <summary>
    /// Property: BoundaryId round-trip through string representation preserves identity.
    /// </summary>
    [Property]
    public Property BoundaryIdRoundTripPreservation(BoundaryId original)
    {
        var str = original.ToString();
        var parsed = BoundaryId.Parse(str);
        return (original == parsed).ToProperty();
    }
}

/// <summary>
/// Registration class for FsCheck arbitraries.
/// </summary>
public static class IdentityArbitrariesRegistration
{
    public static void Register()
    {
        Arb.Register<IdentityArbitraries>();
    }
}
