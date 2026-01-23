using System.Collections.Immutable;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

public class SolverEmissionTests : IDisposable
{
    private static readonly DateTimeOffset FixedTimestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    private readonly PlateTopologyEventStore _store;

    public SolverEmissionTests()
    {
        _store = TestStores.CreateEventStore();
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    #region T0701: Validation Harness Tests

    [Fact]
    public async Task ValidateEventSequence_ValidSequence_MaterializesSuccessfully()
    {
        var stream = CreateTestStream(1);
        var events = CreateBasicValidSequence(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.MaterializedState);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task ValidateEventSequence_WithInvariantViolation_ThrowsInvalidOperationException()
    {
        var stream = CreateTestStream(2);
        var events = Fixture_FR016_BoundaryRetiredWithoutJunctionUpdate(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("FR-016", result.Exception.Message);
    }

    [Fact]
    public async Task ValidateEventSequence_ValidatesRequiredEventTypes()
    {
        var stream = CreateTestStream(3);
        var events = CreateBasicValidSequence(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events,
            CancellationToken.None,
            requiredEventTypes: new[] { "PlateCreatedEvent", "BoundaryCreatedEvent", "JunctionCreatedEvent" });

        Assert.True(result.IsValid);
        Assert.True(result.HasRequiredEventTypes);
    }

    [Fact]
    public async Task ValidateEventSequence_MissingRequiredEventType_FailsValidation()
    {
        var stream = CreateTestStream(4);
        var plateId = new PlateId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                plateId,
                FixedTimestamp,
                0,
                stream)
        };

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events,
            CancellationToken.None,
            requiredEventTypes: new[] { "PlateCreatedEvent", "BoundaryCreatedEvent" });

        Assert.False(result.IsValid);
        Assert.False(result.HasRequiredEventTypes);
        Assert.Contains("BoundaryCreatedEvent", result.MissingEventTypes);
    }

    [Fact]
    public async Task ValidateEventSequence_ContiguousSequenceNumbers_Passes()
    {
        var stream = CreateTestStream(5);
        var events = CreateBasicValidSequence(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.True(result.HasContiguousSequences);
    }

    [Fact]
    public async Task ValidateEventSequence_NonContiguousSequenceNumbers_FailsValidation()
    {
        var stream = CreateTestStream(6);
        var plateId = new PlateId(Guid.Parse("10000000-0000-0000-0000-000000000002"));
        var plateId2 = new PlateId(Guid.Parse("10000000-0000-0000-0000-000000000003"));
        var boundaryId = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-000000000002"));
        var junctionId = new JunctionId(Guid.Parse("40000000-0000-0000-0000-000000000002"));

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
                plateId,
                FixedTimestamp,
                0,
                stream),
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-000000000003"),
                plateId2,
                FixedTimestamp,
                2,
                stream),
            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-000000000002"),
                boundaryId,
                plateId,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                3,
                stream),
            new JunctionCreatedEvent(
                Guid.Parse("40000000-0000-0000-0000-000000000002"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                FixedTimestamp,
                4,
                stream)
        };

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.False(result.HasContiguousSequences);
        Assert.Equal(1, result.SequenceGapStart);
        Assert.Equal(1, result.SequenceGapEnd);
    }

    [Fact]
    public async Task ValidateEventSequence_EmptySequence_MaterializesToEmptyState()
    {
        var stream = CreateTestStream(7);
        var events = Array.Empty<IPlateTopologyEvent>();

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.MaterializedState);
        Assert.Equal(-1, result.MaterializedState.LastEventSequence);
        Assert.Empty(result.MaterializedState.Plates);
        Assert.Empty(result.MaterializedState.Boundaries);
        Assert.Empty(result.MaterializedState.Junctions);
    }

    #endregion

    #region T0702: FR-016 Fixture Tests

    [Fact]
    public async Task Fixture_FR016_BoundaryRetiredWithoutJunctionUpdate_FailsMaterialization()
    {
        var stream = CreateTestStream(100);
        var events = Fixture_FR016_BoundaryRetiredWithoutJunctionUpdate(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);

        var ex = Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("FR-016", ex.Message);
        Assert.Contains("Cannot retire boundary", ex.Message);
    }

    [Fact]
    public async Task Fixture_FR016_BoundaryRetiredAfterJunctionRetired_Succeeds()
    {
        var stream = CreateTestStream(101);
        var events = Fixture_FR016_BoundaryRetiredAfterJunctionRetired(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.MaterializedState);
        Assert.Equal(2, result.MaterializedState.Plates.Count);
        Assert.Single(result.MaterializedState.Boundaries);
        Assert.Single(result.MaterializedState.Junctions);
    }

    [Fact]
    public async Task Fixture_FR016_BoundaryRetiredAfterJunctionUpdated_Succeeds()
    {
        var stream = CreateTestStream(102);
        var events = Fixture_FR016_BoundaryRetiredAfterJunctionUpdated(stream);

        var result = await SolverEmissionTestHelper.ValidateSequenceAsync(
            _store, stream, events, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.MaterializedState);
        Assert.Equal(3, result.MaterializedState.Plates.Count);
        Assert.Equal(2, result.MaterializedState.Boundaries.Count);
        Assert.Single(result.MaterializedState.Junctions);
    }

    #endregion

    #region Fixture Methods

    public static List<IPlateTopologyEvent> Fixture_FR016_BoundaryRetiredWithoutJunctionUpdate(
        TruthStreamIdentity stream)
    {
        var plateId1 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff01"));
        var plateId2 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff02"));
        var boundaryId = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-00000000ff01"));
        var junctionId = new JunctionId(Guid.Parse("40000000-0000-0000-0000-00000000ff01"));

        return new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff01"),
                plateId1,
                FixedTimestamp,
                0,
                stream),

            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff02"),
                plateId2,
                FixedTimestamp,
                1,
                stream),

            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff02"),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                2,
                stream),

            new JunctionCreatedEvent(
                Guid.Parse("40000000-0000-0000-0000-00000000ff02"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                FixedTimestamp,
                3,
                stream),

            new BoundaryRetiredEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff03"),
                boundaryId,
                "premature retirement",
                FixedTimestamp,
                4,
                stream)
        };
    }

    public static List<IPlateTopologyEvent> Fixture_FR016_BoundaryRetiredAfterJunctionRetired(
        TruthStreamIdentity stream)
    {
        var plateId1 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff11"));
        var plateId2 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff12"));
        var boundaryId = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-00000000ff11"));
        var junctionId = new JunctionId(Guid.Parse("40000000-0000-0000-0000-00000000ff11"));

        return new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff11"),
                plateId1,
                FixedTimestamp,
                0,
                stream),

            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff12"),
                plateId2,
                FixedTimestamp,
                1,
                stream),

            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff12"),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                2,
                stream),

            new JunctionCreatedEvent(
                Guid.Parse("40000000-0000-0000-0000-00000000ff12"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                FixedTimestamp,
                3,
                stream),

            new JunctionRetiredEvent(
                Guid.Parse("40000000-0000-0000-0000-00000000ff13"),
                junctionId,
                "junction removed",
                FixedTimestamp,
                4,
                stream),

            new BoundaryRetiredEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff13"),
                boundaryId,
                "boundary retired",
                FixedTimestamp,
                5,
                stream)
        };
    }

    public static List<IPlateTopologyEvent> Fixture_FR016_BoundaryRetiredAfterJunctionUpdated(
        TruthStreamIdentity stream)
    {
        var plateId1 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff21"));
        var plateId2 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff22"));
        var plateId3 = new PlateId(Guid.Parse("10000000-0000-0000-0000-00000000ff23"));
        var boundaryId1 = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-00000000ff21"));
        var boundaryId2 = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-00000000ff22"));
        var junctionId = new JunctionId(Guid.Parse("40000000-0000-0000-0000-00000000ff21"));

        return new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff21"),
                plateId1,
                FixedTimestamp,
                0,
                stream),

            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff22"),
                plateId2,
                FixedTimestamp,
                1,
                stream),

            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-00000000ff23"),
                plateId3,
                FixedTimestamp,
                2,
                stream),

            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff22"),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                3,
                stream),

            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff23"),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Transform,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                FixedTimestamp,
                4,
                stream),

            new JunctionCreatedEvent(
                Guid.Parse("40000000-0000-0000-0000-00000000ff22"),
                junctionId,
                [boundaryId1, boundaryId2],
                new Point2D(1.0, 0.0),
                FixedTimestamp,
                5,
                stream),

            new JunctionUpdatedEvent(
                Guid.Parse("40000000-0000-0000-0000-00000000ff23"),
                junctionId,
                [boundaryId2],
                null,
                FixedTimestamp,
                6,
                stream),

            new BoundaryRetiredEvent(
                Guid.Parse("30000000-0000-0000-0000-00000000ff23"),
                boundaryId1,
                "boundary retired",
                FixedTimestamp,
                7,
                stream)
        };
    }

    #endregion

    #region Helper Methods

    private static TruthStreamIdentity CreateTestStream(int suffix)
    {
        return new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            suffix.ToString());
    }

    private static List<IPlateTopologyEvent> CreateBasicValidSequence(TruthStreamIdentity stream)
    {
        var plateId1 = new PlateId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var plateId2 = new PlateId(Guid.Parse("10000000-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        var junctionId = new JunctionId(Guid.Parse("40000000-0000-0000-0000-000000000001"));

        return new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                plateId1,
                FixedTimestamp,
                0,
                stream),

            new PlateCreatedEvent(
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
                plateId2,
                FixedTimestamp,
                1,
                stream),

            new BoundaryCreatedEvent(
                Guid.Parse("30000000-0000-0000-0000-000000000002"),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                2,
                stream),

            new JunctionCreatedEvent(
                Guid.Parse("40000000-0000-0000-0000-000000000002"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                FixedTimestamp,
                3,
                stream)
        };
    }

    #endregion
}

internal static class SolverEmissionTestHelper
{
    public record ValidationResult
    {
        public bool MaterializationSucceeded { get; init; }
        public bool HasRequiredEventTypes { get; init; } = true;
        public bool HasContiguousSequences { get; init; } = true;
        public bool IsValid => MaterializationSucceeded && HasRequiredEventTypes && HasContiguousSequences;

        public PlateTopologyState? MaterializedState { get; init; }
        public Exception? Exception { get; init; }
        public ImmutableArray<string> MissingEventTypes { get; init; } = ImmutableArray<string>.Empty;
        public int SequenceGapStart { get; init; } = -1;
        public int SequenceGapEnd { get; init; } = -1;
    }

    public static async Task<ValidationResult> ValidateSequenceAsync(
        ITopologyEventStore store,
        TruthStreamIdentity stream,
        IReadOnlyList<IPlateTopologyEvent> events,
        CancellationToken cancellationToken,
        string[]? requiredEventTypes = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);

        var result = new ValidationResult();

        var (hasContiguous, gapStart, gapEnd) = ValidateSequenceContiguity(events);
        result = result with { HasContiguousSequences = hasContiguous, SequenceGapStart = gapStart, SequenceGapEnd = gapEnd };

        if (requiredEventTypes != null && requiredEventTypes.Length > 0)
        {
            var (hasRequired, missingTypes) = ValidateRequiredEventTypes(events, requiredEventTypes);
            result = result with { HasRequiredEventTypes = hasRequired, MissingEventTypes = missingTypes };
        }

        try
        {
            await store.AppendAsync(stream, events, cancellationToken);

            var materializer = new PlateTopologyMaterializer(store);
            var materializedState = await materializer.MaterializeAsync(stream, cancellationToken);

            result = result with { MaterializationSucceeded = true, MaterializedState = materializedState };
        }
        catch (InvalidOperationException ex)
        {
            result = result with { MaterializationSucceeded = false, Exception = ex };
        }
        catch (Exception ex)
        {
            result = result with { MaterializationSucceeded = false, Exception = ex };
        }

        return result;
    }

    private static (bool HasContiguous, int GapStart, int GapEnd) ValidateSequenceContiguity(
        IReadOnlyList<IPlateTopologyEvent> events)
    {
        if (events.Count == 0)
            return (true, -1, -1);

        long expectedSequence = 0;

        foreach (var evt in events)
        {
            if (evt.Sequence != expectedSequence)
            {
                return (false, (int)expectedSequence, (int)evt.Sequence - 1);
            }

            expectedSequence++;
        }

        return (true, -1, -1);
    }

    private static (bool HasRequired, ImmutableArray<string> MissingTypes) ValidateRequiredEventTypes(
        IReadOnlyList<IPlateTopologyEvent> events,
        string[] requiredEventTypes)
    {
        var presentTypes = events
            .Select(e => e.EventType)
            .ToImmutableHashSet();

        var missing = requiredEventTypes
            .Where(t => !presentTypes.Contains(t))
            .ToImmutableArray();

        return (missing.IsEmpty, missing);
    }
}
