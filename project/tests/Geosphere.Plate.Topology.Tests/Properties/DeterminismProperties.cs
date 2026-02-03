using Plate.TimeDete.Determinism.Pcg;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Properties;

/// <summary>
/// Property-based tests for ID generation determinism per RFC-V2-0001.
/// </summary>
public class DeterminismProperties
{
    /// <summary>
    /// Property: Same seed must produce identical PlateId sequences.
    /// This is the core determinism guarantee for replayability.
    /// </summary>
    [Fact]
    public void SameSeedProducesIdenticalPlateIdSequences()
    {
        var rng = new Random(12345);

        for (var i = 0; i < 50; i++)
        {
            var seed = (ulong)rng.NextInt64();
            var constrainedCount = rng.Next(1, 101);

            var rng1 = new PcgSeededRngFactory().Create(seed);
            var rng2 = new PcgSeededRngFactory().Create(seed);

            var ids1 = Enumerable.Range(0, constrainedCount).Select(_ => PlateId.NewId(rng1));
            var ids2 = Enumerable.Range(0, constrainedCount).Select(_ => PlateId.NewId(rng2));

            ids1.SequenceEqual(ids2).Should().BeTrue();
        }
    }

    /// <summary>
    /// Property: Same seed must produce identical BoundaryId sequences.
    /// </summary>
    [Fact]
    public void SameSeedProducesIdenticalBoundaryIdSequences()
    {
        var rng = new Random(23456);

        for (var i = 0; i < 50; i++)
        {
            var seed = (ulong)rng.NextInt64();
            var constrainedCount = rng.Next(1, 101);

            var rng1 = new PcgSeededRngFactory().Create(seed);
            var rng2 = new PcgSeededRngFactory().Create(seed);

            var ids1 = Enumerable.Range(0, constrainedCount).Select(_ => BoundaryId.NewId(rng1));
            var ids2 = Enumerable.Range(0, constrainedCount).Select(_ => BoundaryId.NewId(rng2));

            ids1.SequenceEqual(ids2).Should().BeTrue();
        }
    }

    /// <summary>
    /// Property: PlateId round-trip through string representation preserves identity.
    /// </summary>
    [Fact]
    public void PlateIdRoundTripPreservation()
    {
        var rng = new Random(34567);

        for (var i = 0; i < 500; i++)
        {
            var original = new PlateId(Guid.NewGuid());
            var str = original.ToString();
            var parsed = PlateId.Parse(str);
            parsed.Should().Be(original);
        }
    }

    /// <summary>
    /// Property: BoundaryId round-trip through string representation preserves identity.
    /// </summary>
    [Fact]
    public void BoundaryIdRoundTripPreservation()
    {
        var rng = new Random(45678);

        for (var i = 0; i < 500; i++)
        {
            var original = new BoundaryId(Guid.NewGuid());
            var str = original.ToString();
            var parsed = BoundaryId.Parse(str);
            parsed.Should().Be(original);
        }
    }
}
