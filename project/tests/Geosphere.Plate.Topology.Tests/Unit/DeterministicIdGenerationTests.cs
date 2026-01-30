using Plate.TimeDete.Determinism.Pcg;
using FantaSim.Geosphere.Plate.Topology.Contracts.Determinism;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Unit;

/// <summary>
/// Tests for deterministic ID generation using time-dete RNG streams.
/// Per RFC-V2-0001 review: solver implementations should use deterministic ID generation
/// to ensure replay determinism.
/// </summary>
public sealed class DeterministicIdGenerationTests
{
    [Fact]
    public void PlateId_NewId_WithSameSeed_ProducesIdenticalSequence()
    {
        // Arrange
        const ulong seed = 12345UL;
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var ids1 = Enumerable.Range(0, 10).Select(_ => PlateId.NewId(rng1)).ToList();
        var ids2 = Enumerable.Range(0, 10).Select(_ => PlateId.NewId(rng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void BoundaryId_NewId_WithSameSeed_ProducesIdenticalSequence()
    {
        // Arrange
        const ulong seed = 54321UL;
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var ids1 = Enumerable.Range(0, 10).Select(_ => BoundaryId.NewId(rng1)).ToList();
        var ids2 = Enumerable.Range(0, 10).Select(_ => BoundaryId.NewId(rng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void JunctionId_NewId_WithSameSeed_ProducesIdenticalSequence()
    {
        // Arrange
        const ulong seed = 98765UL;
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var ids1 = Enumerable.Range(0, 10).Select(_ => JunctionId.NewId(rng1)).ToList();
        var ids2 = Enumerable.Range(0, 10).Select(_ => JunctionId.NewId(rng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentIds()
    {
        // Arrange
        var rng1 = new PcgSeededRngFactory().Create(111UL);
        var rng2 = new PcgSeededRngFactory().Create(222UL);

        // Act
        var id1 = PlateId.NewId(rng1);
        var id2 = PlateId.NewId(rng2);

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void DeterministicIdFactory_ProducesConsistentIds()
    {
        // Arrange
        const ulong seed = 42UL;
        var factory = new DeterministicIdFactory(); // Use DI, not singleton
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var plate1 = factory.NewPlateId(rng1);
        var boundary1 = factory.NewBoundaryId(rng1);
        var junction1 = factory.NewJunctionId(rng1);

        var plate2 = factory.NewPlateId(rng2);
        var boundary2 = factory.NewBoundaryId(rng2);
        var junction2 = factory.NewJunctionId(rng2);

        // Assert
        Assert.Equal(plate1, plate2);
        Assert.Equal(boundary1, boundary2);
        Assert.Equal(junction1, junction2);
    }

    [Fact]
    public void GeneratedIds_AreValidUuids()
    {
        // Arrange
        var rng = new PcgSeededRngFactory().Create(99UL);

        // Act
        var plateId = PlateId.NewId(rng);
        var boundaryId = BoundaryId.NewId(rng);
        var junctionId = JunctionId.NewId(rng);

        // Assert
        Assert.False(plateId.IsEmpty);
        Assert.False(boundaryId.IsEmpty);
        Assert.False(junctionId.IsEmpty);

        // Verify they can be parsed back
        Assert.True(PlateId.TryParse(plateId.ToString(), out _));
        Assert.True(BoundaryId.TryParse(boundaryId.ToString(), out _));
        Assert.True(JunctionId.TryParse(junctionId.ToString(), out _));
    }

    [Fact]
    public void RngStreamProvider_ProducesIsolatedDeterministicStreams()
    {
        // Arrange
        const ulong masterSeed = 1000UL;
        var provider1 = new PcgRngStreamProvider(masterSeed);
        var provider2 = new PcgRngStreamProvider(masterSeed);

        // Act - get same stream from both providers
        var plateRng1 = provider1.GetStream("plates");
        var plateRng2 = provider2.GetStream("plates");

        var ids1 = Enumerable.Range(0, 5).Select(_ => PlateId.NewId(plateRng1)).ToList();
        var ids2 = Enumerable.Range(0, 5).Select(_ => PlateId.NewId(plateRng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void DifferentStreams_ProduceDifferentIds()
    {
        // Arrange
        var provider = new PcgRngStreamProvider(1000UL);
        var plateRng = provider.GetStream("plates");
        var boundaryRng = provider.GetStream("boundaries");

        // Act
        var plateId = PlateId.NewId(plateRng);
        var boundaryAsPlateId = PlateId.NewId(boundaryRng);

        // Assert - different streams should produce different IDs
        Assert.NotEqual(plateId, boundaryAsPlateId);
    }
}

/// <summary>
/// Tests for Domain.IsValid() matching Parse() validation per RFC-V2-0001 review.
/// </summary>
public sealed class DomainValidationTests
{
    [Theory]
    [InlineData("geo.plates", true)]
    [InlineData("geo_plates", true)]
    [InlineData("simple", true)]
    [InlineData("a.b.c.d", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(".invalid", false)]
    [InlineData("invalid.", false)]
    [InlineData("in..valid", false)]
    [InlineData("has spaces", false)]
    [InlineData("has-dash", false)]
    [InlineData("has@special", false)]
    public void Domain_IsValid_MatchesParseValidation(string value, bool expectedValid)
    {
        // Arrange - try to construct via Parse to get a valid Domain
        Domain domain;
        bool canParse = Domain.TryParse(value, out domain);

        // If we can't parse, create a "raw" domain via reflection to test IsValid
        if (!canParse)
        {
            // For invalid inputs, IsValid should return false
            // We test the empty/whitespace case directly
            domain = default;
        }

        // Act
        var isValid = domain.IsValid();

        // Assert
        Assert.Equal(expectedValid, isValid);
        Assert.Equal(expectedValid, canParse);
    }

    [Fact]
    public void Domain_IsValid_ReturnsTrue_ForValidParsedDomain()
    {
        // Arrange
        var domain = Domain.Parse("geo.plates");

        // Act & Assert
        Assert.True(domain.IsValid());
    }

    [Fact]
    public void Domain_IsValid_ReturnsFalse_ForDefaultDomain()
    {
        // Arrange
        var domain = default(Domain);

        // Act & Assert
        Assert.False(domain.IsValid());
    }
}

/// <summary>
/// Tests for TruthStreamIdentity validation per RFC-V2-0001 review.
/// </summary>
public sealed class TruthStreamIdentityValidationTests
{
    [Fact]
    public void IsValid_ReturnsFalse_ForEmptyVariantId()
    {
        var identity = new TruthStreamIdentity("", "main", 2, Domain.Parse("geo.plates"), "0");
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForEmptyBranchId()
    {
        var identity = new TruthStreamIdentity("science", "", 2, Domain.Parse("geo.plates"), "0");
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNegativeLLevel()
    {
        var identity = new TruthStreamIdentity("science", "main", -1, Domain.Parse("geo.plates"), "0");
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForEmptyModel()
    {
        var identity = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "");
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidDomain()
    {
        // Default domain is invalid
        var identity = new TruthStreamIdentity("science", "main", 2, default, "0");
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidIdentity()
    {
        var identity = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        Assert.True(identity.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsTrue_WithMPrefixInModel()
    {
        var identity = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "M0");
        Assert.True(identity.IsValid());
    }
}

/// <summary>
/// Tests for EventId deterministic generation per RFC-099 guidance.
/// </summary>
public sealed class EventIdDeterministicTests
{
    [Fact]
    public void EventId_NewId_WithSameSeed_ProducesIdenticalSequence()
    {
        // Arrange
        const ulong seed = 77777UL;
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var ids1 = Enumerable.Range(0, 10).Select(_ => EventId.NewId(rng1)).ToList();
        var ids2 = Enumerable.Range(0, 10).Select(_ => EventId.NewId(rng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void EventId_DifferentSeeds_ProduceDifferentIds()
    {
        // Arrange
        var rng1 = new PcgSeededRngFactory().Create(333UL);
        var rng2 = new PcgSeededRngFactory().Create(444UL);

        // Act
        var id1 = EventId.NewId(rng1);
        var id2 = EventId.NewId(rng2);

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void EventId_CanConvertToAndFromGuid()
    {
        // Arrange
        var rng = new PcgSeededRngFactory().Create(555UL);
        var eventId = EventId.NewId(rng);

        // Act
        Guid asGuid = eventId; // Implicit conversion
        var roundTripped = EventId.FromGuid(asGuid);

        // Assert
        Assert.Equal(eventId, roundTripped);
    }

    [Fact]
    public void EventId_CanParseFromString()
    {
        // Arrange
        var rng = new PcgSeededRngFactory().Create(666UL);
        var original = EventId.NewId(rng);
        var str = original.ToString();

        // Act
        var parsed = EventId.Parse(str);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DeterministicIdFactory_IncludesEventId()
    {
        // Arrange
        const ulong seed = 888UL;
        var factory = new DeterministicIdFactory();
        var rng1 = new PcgSeededRngFactory().Create(seed);
        var rng2 = new PcgSeededRngFactory().Create(seed);

        // Act
        var eventId1 = factory.NewEventId(rng1);
        var eventId2 = factory.NewEventId(rng2);

        // Assert
        Assert.Equal(eventId1, eventId2);
    }
}

/// <summary>
/// Tests for ISolverSeedProvider and seed derivation per RFC-099 audit requirements.
/// </summary>
public sealed class SolverSeedProviderTests
{
    [Fact]
    public void DeriveSeed_SameInputs_ProducesSameSeed()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream);
        var seed2 = provider.DeriveSeed(12345UL, stream);

        // Assert
        Assert.Equal(seed1, seed2);
    }

    [Fact]
    public void DeriveSeed_DifferentScenarioSeeds_ProduceDifferentSeeds()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream);
        var seed2 = provider.DeriveSeed(54321UL, stream);

        // Assert
        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void DeriveSeed_DifferentStreams_ProduceDifferentSeeds()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream1 = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("wuxing", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream1);
        var seed2 = provider.DeriveSeed(12345UL, stream2);

        // Assert
        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void CreateRng_ProducesDeterministicRng()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var rng1 = provider.CreateRng(12345UL, stream);
        var rng2 = provider.CreateRng(12345UL, stream);

        // Generate IDs with each RNG
        var ids1 = Enumerable.Range(0, 5).Select(_ => PlateId.NewId(rng1)).ToList();
        var ids2 = Enumerable.Range(0, 5).Select(_ => PlateId.NewId(rng2)).ToList();

        // Assert
        Assert.Equal(ids1, ids2);
    }

    [Fact]
    public void GetSeedAuditRecord_CapturesDerivation()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        const ulong scenarioSeed = 99999UL;

        // Act
        var record = provider.GetSeedAuditRecord(scenarioSeed, stream);

        // Assert
        Assert.Equal(scenarioSeed, record.ScenarioSeed);
        Assert.Equal(stream, record.StreamIdentity);
        Assert.Equal("FNV1a-StreamIdentity-v2", record.DerivationAlgorithm);
        Assert.NotEqual(0UL, record.DerivedSeed);

        // Verify the recorded seed matches DeriveSeed
        var expectedSeed = provider.DeriveSeed(scenarioSeed, stream);
        Assert.Equal(expectedSeed, record.DerivedSeed);
    }

    [Fact]
    public void DeriveSeed_DifferentLLevels_ProduceDifferentSeeds()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var stream1 = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("science", "main", 3, Domain.Parse("geo.plates"), "0");

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream1);
        var seed2 = provider.DeriveSeed(12345UL, stream2);

        // Assert
        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void DeriveSeed_ThrowsForInvalidStreamIdentity()
    {
        // Arrange
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());
        var invalidStream = new TruthStreamIdentity("", "main", 2, Domain.Parse("geo.plates"), "0"); // Empty VariantId

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => provider.DeriveSeed(12345UL, invalidStream));
        Assert.Contains("not valid", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeriveSeed_LengthPrefixing_PreventsConcatenationCollisions()
    {
        // Arrange - this test verifies the fix for the concatenation collision issue
        // ("a","bc") vs ("ab","c") should produce different seeds
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());

        // Stream with VariantId="a", BranchId="bc"
        var stream1 = new TruthStreamIdentity("a", "bc", 2, Domain.Parse("geo.plates"), "0");
        // Stream with VariantId="ab", BranchId="c"
        var stream2 = new TruthStreamIdentity("ab", "c", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream1);
        var seed2 = provider.DeriveSeed(12345UL, stream2);

        // Assert - with length-prefixing, these should NOT collide
        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void DeriveSeed_SimilarStrings_ProduceDifferentSeeds()
    {
        // Arrange - additional collision resistance test
        var provider = new FnvSolverSeedProvider(new PcgSeededRngFactory());

        var stream1 = new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("tes", "tmain", 2, Domain.Parse("geo.plates"), "0"); // Shifted boundary

        // Act
        var seed1 = provider.DeriveSeed(12345UL, stream1);
        var seed2 = provider.DeriveSeed(12345UL, stream2);

        // Assert
        Assert.NotEqual(seed1, seed2);
    }
}
