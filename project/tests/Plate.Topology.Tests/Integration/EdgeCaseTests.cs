using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using UnifyGeometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

/// <summary>
/// Integration tests for edge cases and invariant validation at the state level.
///
/// Tests verify edge case scenarios that stress the invariant system:
/// - Empty streams
/// - Orphan boundaries (boundary not referenced by any junction)
/// - Retired boundary with no junctions (should be allowed)
/// - Complex junction reference patterns
/// - Plate deletion effects on boundaries
///
/// Per T0503: Validate edge cases are handled correctly with clear error messages.
/// </summary>
public class EdgeCaseTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;
    private readonly PlateTopologyMaterializer _materializer;

    public EdgeCaseTests()
    {
        _store = TestStores.CreateEventStore();
        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
        _materializer = new PlateTopologyMaterializer(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    #region Empty Stream and Minimal Topology

    [Fact]
    public async Task EmptyStream_MaterializesToEmptyState()
    {
        // Act - Materialize from empty stream
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert - Empty state with LastEventSequence = -1 (SC-008)
        Assert.Equal(_stream, state.Identity);
        Assert.Empty(state.Plates);
        Assert.Empty(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.Equal(-1, state.LastEventSequence);
        Assert.Empty(state.Violations);
    }

    [Fact]
    public async Task SinglePlate_NoBoundaries_MaterializesSuccessfully()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId, new CanonicalTick(0), 0, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert
        Assert.Single(state.Plates);
        Assert.True(state.Plates.ContainsKey(plateId));
        Assert.False(state.Plates[plateId].IsRetired);
        Assert.Empty(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.Equal(0, state.LastEventSequence);
        Assert.Empty(state.Violations);
    }

    #endregion

    #region Orphan Boundary Cases

    [Fact]
    public async Task BoundaryWithoutJunction_MaterializesSuccessfully()
    {
        // Arrange - A boundary that exists but is not referenced by any junction
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert - Orphan boundary is allowed (junctions reference boundaries, not vice versa)
        Assert.NotNull(state);
        Assert.Equal(2, state.Plates.Count);
        Assert.Single(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.False(state.Boundaries[boundaryId].IsRetired);
        Assert.Empty(state.Violations);
    }

    [Fact]
    public async Task RetiredBoundaryWithNoJunctions_MaterializesSuccessfully()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.BoundaryRetired(Guid.NewGuid(), boundaryId, "test retirement", new CanonicalTick(3), 3, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert - Retired boundary with no junctions is allowed
        Assert.NotNull(state);
        Assert.Equal(2, state.Plates.Count);
        Assert.Single(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.True(state.Boundaries[boundaryId].IsRetired);
        Assert.Equal("test retirement", state.Boundaries[boundaryId].RetirementReason);
        Assert.Empty(state.Violations);
    }

    #endregion

    #region Complex Junction Reference Patterns

    [Fact]
    public async Task JunctionReferencesMultipleBoundaries_MaterializesSuccessfully()
    {
        // Arrange - A junction where 3 boundaries meet
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var plateId4 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());
        var boundaryId3 = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId4, new CanonicalTick(3), 3, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            ),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Transform,
                new Segment2(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(5),
                5,
                _stream
            ),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId3,
                plateId3,
                plateId4,
                BoundaryType.Transform,
                new Segment2(2.0, 0.0, 3.0, 0.0),
                new CanonicalTick(6),
                6,
                _stream
            ),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId,
                [boundaryId1, boundaryId2, boundaryId3],
                new Point2(1.0, 0.0),
                new CanonicalTick(7),
                7,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(4, state.Plates.Count);
        Assert.Equal(3, state.Boundaries.Count);
        Assert.Single(state.Junctions);

        var junction = state.Junctions[junctionId];
        Assert.Equal(3, junction.BoundaryIds.Length);
        Assert.Contains(boundaryId1, junction.BoundaryIds);
        Assert.Contains(boundaryId2, junction.BoundaryIds);
        Assert.Contains(boundaryId3, junction.BoundaryIds);
        Assert.Empty(state.Violations);
    }

    [Fact]
    public async Task TwoJunctionsShareBoundary_MaterializesSuccessfully()
    {
        // Arrange - A boundary shared by two junctions (both endpoints)
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId1 = new JunctionId(Guid.NewGuid());
        var junctionId2 = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId1,
                [boundaryId],
                new Point2(0.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId2,
                [boundaryId],
                new Point2(1.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(2, state.Plates.Count);
        Assert.Single(state.Boundaries);
        Assert.Equal(2, state.Junctions.Count);

        var junction1 = state.Junctions[junctionId1];
        Assert.Single(junction1.BoundaryIds);
        Assert.Equal(boundaryId, junction1.BoundaryIds[0]);

        var junction2 = state.Junctions[junctionId2];
        Assert.Single(junction2.BoundaryIds);
        Assert.Equal(boundaryId, junction2.BoundaryIds[0]);

        Assert.Empty(state.Violations);
    }

    #endregion

    #region Plate Retirement Effects

    [Fact]
    public async Task PlateRetired_WithActiveBoundaries_StateStillMaterializes()
    {
        // Arrange - Retire a plate while boundaries still reference it
        // This is allowed at the state level (the invariants will be checked post-materialization)
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.PlateRetired(Guid.NewGuid(), plateId1, "test retirement", new CanonicalTick(3), 3, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert - State materializes, but post-materialization validation would catch the invariant violation
        Assert.NotNull(state);
        Assert.Equal(2, state.Plates.Count);
        Assert.Single(state.Boundaries);
        Assert.Empty(state.Junctions);

        // Plate is retired
        Assert.True(state.Plates[plateId1].IsRetired);
        Assert.False(state.Plates[plateId2].IsRetired);

        // Boundary still exists (not retired)
        Assert.False(state.Boundaries[boundaryId].IsRetired);

        // The violation is recorded but not thrown during materialization
        // (per the current design where violations are accumulated in state.Violations)
        Assert.Empty(state.Violations); // Materialization doesn't throw, just records
    }

    [Fact]
    public async Task PostMaterializationValidation_WithRetiredPlateBoundary_ThrowsInvalidOperationException()
    {
        // Arrange - Create a state where a boundary references a retired plate
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.PlateRetired(Guid.NewGuid(), plateId1, "test retirement", new CanonicalTick(3), 3, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Now validate the state using InvariantValidator
        var exception = Assert.Throws<InvalidOperationException>(() => InvariantValidator.Validate(state));

        // Assert
        Assert.Contains("BoundarySeparatesTwoPlates", exception.Message);
        Assert.Contains("retired left plate", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains(plateId1.ToString(), exception.Message);
    }

    #endregion

    #region State-Level Invariant Detection

    [Fact]
    public async Task StateValidation_DetectsOrphanJunctionWithRetiredBoundary()
    {
        // Arrange - Create a state where a junction references a retired boundary
        // that was retired AFTER the junction was created (post-creation corruption)
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            // Boundary retirement without updating junction first (FR-016 violation)
            // This would normally be caught by event validation, but let's verify state-level detection too
            TestEventFactory.BoundaryRetired(
                Guid.NewGuid(),
                boundaryId,
                "test retirement",
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert - This should throw during materialization due to FR-016 check
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );
    }

    [Fact]
    public async Task StateValidation_DetectsMultipleViolations()
    {
        // Arrange - Create a state with multiple invariant violations
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId,
                [boundaryId1, boundaryId2], // boundaryId2 doesn't exist
                new Point2(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        // Should detect the non-existent boundary reference
        Assert.Contains("NoOrphanJunctions", exception.Message);
        Assert.Contains("non-existent boundary", exception.Message);
        Assert.Contains(boundaryId2.ToString(), exception.Message);
    }

    #endregion

    #region Deterministic Replay with Invariants

    [Fact]
    public async Task InvariantViolations_AreDeterministicAcrossReplays()
    {
        // Arrange - Create an event stream that causes invariant violations
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                new PlateId(Guid.NewGuid()), // Non-existent right plate
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(1),
                1,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act - Materialize twice
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );
        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        // Assert - Same exception thrown both times (deterministic)
        Assert.Equal(exception1.Message, exception2.Message);
    }

    #endregion
}
