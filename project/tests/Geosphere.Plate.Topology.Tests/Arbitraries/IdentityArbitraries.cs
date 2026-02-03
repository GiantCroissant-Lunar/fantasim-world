using FsCheck;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Arbitraries;

/// <summary>
/// Custom FsCheck Arbitrary generators for topology identity types.
/// </summary>
public static class IdentityArbitraries
{
    /// <summary>
    /// Generates arbitrary non-empty PlateId values.
    /// </summary>
    public static Arbitrary<PlateId> PlateId()
    {
        return Arb.Default.Guid()
            .Generator
            .Where(g => g != Guid.Empty)
            .Select(g => new PlateId(g))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates arbitrary non-empty BoundaryId values.
    /// </summary>
    public static Arbitrary<BoundaryId> BoundaryId()
    {
        return Arb.Default.Guid()
            .Generator
            .Where(g => g != Guid.Empty)
            .Select(g => new BoundaryId(g))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates arbitrary seed values for deterministic ID generation.
    /// </summary>
    public static Arbitrary<ulong> Seed => Arb.Default.UInt64();

    /// <summary>
    /// Generates tuples of (seed, count) for testing ID sequences.
    /// Count is constrained to reasonable values (1-100) for test performance.
    /// </summary>
    public static Arbitrary<(ulong Seed, int Count)> SeedWithCount()
    {
        return Arb.Default.UInt64()
            .Generator
            .SelectMany(seed =>
                Gen.Choose(1, 100).Select(count => (seed, count)))
            .ToArbitrary();
    }
}
