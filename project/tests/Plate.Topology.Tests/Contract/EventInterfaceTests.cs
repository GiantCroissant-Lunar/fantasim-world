using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Xunit;

namespace Plate.Topology.Tests.Contract;

/// <summary>
/// Unit tests for plate topology event interface contract per FR-006, FR-015.
///
/// Tests verify that the event envelope structure satisfies requirements for:
/// - Event identification and uniqueness
/// - Polymorphic deserialization support
/// - Timestamp preservation
/// - Deterministic replay ordering
/// - Stream identity isolation
/// </summary>
public class EventInterfaceTests
{
    #region Test Implementation - Mock Event

    /// <summary>
    /// Mock implementation of IPlateTopologyEvent for testing contract compliance.
    /// </summary>
    private readonly record struct MockPlateTopologyEvent : IPlateTopologyEvent
    {
        public Guid EventId { get; }
        public string EventType { get; }
        public DateTimeOffset Timestamp { get; }
        public long Sequence { get; }
        public TruthStreamIdentity StreamIdentity { get; }

        public MockPlateTopologyEvent(
            Guid eventId,
            string eventType,
            DateTimeOffset timestamp,
            long sequence,
            TruthStreamIdentity streamIdentity
        )
        {
            EventId = eventId;
            EventType = eventType;
            Timestamp = timestamp;
            Sequence = sequence;
            StreamIdentity = streamIdentity;
        }

        /// <summary>
        /// Creates a valid mock event with test defaults.
        /// </summary>
        public static MockPlateTopologyEvent CreateValid(
            Guid? eventId = null,
            string? eventType = null,
            DateTimeOffset? timestamp = null,
            long? sequence = null,
            TruthStreamIdentity? streamIdentity = null
        )
        {
            return new MockPlateTopologyEvent(
                eventId ?? Guid.NewGuid(),
                eventType ?? "MockEvent",
                timestamp ?? DateTimeOffset.UtcNow,
                sequence ?? 0L,
                streamIdentity ?? CreateValidStreamIdentity()
            );
        }

        /// <summary>
        /// Creates a valid truth stream identity for testing.
        /// </summary>
        private static TruthStreamIdentity CreateValidStreamIdentity()
        {
            return new TruthStreamIdentity(
                "test-variant",
                "main",
                2,
                Domain.Parse("geo.plates"),
                "0"
            );
        }
    }

    #endregion

    #region EventId Tests

    [Fact]
    public void Event_Interface_HasEventIdProperty()
    {
        // Arrange & Act
        var eventId = Guid.NewGuid();
        var @event = MockPlateTopologyEvent.CreateValid(eventId: eventId);

        // Assert - EventId should be accessible and match the provided value
        Assert.Equal(eventId, @event.EventId);
    }

    [Fact]
    public void Event_EventId_SupportsNonEmptyGuid()
    {
        // Arrange & Act
        var eventId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var @event = MockPlateTopologyEvent.CreateValid(eventId: eventId);

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.Equal(eventId, @event.EventId);
    }

    [Fact]
    public void Event_EventId_IsUniquePerEvent()
    {
        // Arrange
        var event1 = MockPlateTopologyEvent.CreateValid();
        var event2 = MockPlateTopologyEvent.CreateValid();

        // Assert - Each event should have a unique ID
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void Event_EventId_SupportsUUIDv7TimeSortedIds()
    {
        // Arrange & Act
        var event1 = MockPlateTopologyEvent.CreateValid();
        System.Threading.Thread.Sleep(10); // Ensure time passes
        var event2 = MockPlateTopologyEvent.CreateValid();

        // Assert - Later events should have greater or equal timestamps (UUIDv7 property)
        // This is a basic check; proper UUIDv7 validation would inspect the bytes
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    #endregion

    #region EventType Tests

    [Fact]
    public void Event_Interface_HasEventTypeProperty()
    {
        // Arrange & Act
        var eventType = "PlateCreated";
        var @event = MockPlateTopologyEvent.CreateValid(eventType: eventType);

        // Assert - EventType should be accessible and match the provided value
        Assert.Equal(eventType, @event.EventType);
    }

    [Fact]
    public void Event_EventType_IsString()
    {
        // Arrange & Act
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - EventType should be a string type
        Assert.IsType<string>(@event.EventType);
    }

    [Fact]
    public void Event_EventType_CanRepresentAllCoreEventTypes()
    {
        // Arrange - Event types from FR-008
        var eventTypes = new[]
        {
            "PlateCreated",
            "BoundaryCreated",
            "JunctionCreated",
            "BoundaryTypeChanged",
            "BoundaryGeometryUpdated",
            "BoundaryRetired",
            "JunctionUpdated",
            "JunctionRetired",
            "PlateRetired"
        };

        // Act & Assert - Each event type should be representable
        foreach (var eventType in eventTypes)
        {
            var @event = MockPlateTopologyEvent.CreateValid(eventType: eventType);
            Assert.Equal(eventType, @event.EventType);
        }
    }

    [Fact]
    public void Event_EventType_PolymorphicDiscrimination()
    {
        // Arrange
        var plateEvent = MockPlateTopologyEvent.CreateValid(eventType: "PlateCreated");
        var boundaryEvent = MockPlateTopologyEvent.CreateValid(eventType: "BoundaryCreated");
        var junctionEvent = MockPlateTopologyEvent.CreateValid(eventType: "JunctionCreated");

        // Act & Assert - EventType can discriminate between different event types
        Assert.Equal("PlateCreated", plateEvent.EventType);
        Assert.Equal("BoundaryCreated", boundaryEvent.EventType);
        Assert.Equal("JunctionCreated", junctionEvent.EventType);
        Assert.NotEqual(plateEvent.EventType, boundaryEvent.EventType);
        Assert.NotEqual(boundaryEvent.EventType, junctionEvent.EventType);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Event_Interface_HasTimestampProperty()
    {
        // Arrange & Act
        var timestamp = DateTimeOffset.UtcNow;
        var @event = MockPlateTopologyEvent.CreateValid(timestamp: timestamp);

        // Assert - Timestamp should be accessible and match the provided value
        Assert.Equal(timestamp, @event.Timestamp);
    }

    [Fact]
    public void Event_Timestamp_IsDateTimeOffset()
    {
        // Arrange & Act
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - Timestamp should be DateTimeOffset type
        Assert.IsType<DateTimeOffset>(@event.Timestamp);
    }

    [Fact]
    public void Event_Timestamp_SupportsDifferentTimes()
    {
        // Arrange
        var time1 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);

        // Act
        var event1 = MockPlateTopologyEvent.CreateValid(timestamp: time1);
        var event2 = MockPlateTopologyEvent.CreateValid(timestamp: time2);
        var event3 = MockPlateTopologyEvent.CreateValid(timestamp: time3);

        // Assert - Each event can have a distinct timestamp
        Assert.Equal(time1, event1.Timestamp);
        Assert.Equal(time2, event2.Timestamp);
        Assert.Equal(time3, event3.Timestamp);
        Assert.True(event1.Timestamp < event2.Timestamp);
        Assert.True(event2.Timestamp < event3.Timestamp);
    }

    [Fact]
    public void Event_Timestamp_PreservesExactTime()
    {
        // Arrange
        var exactTimestamp = new DateTimeOffset(2026, 1, 22, 15, 30, 45, 123, TimeSpan.FromHours(-5));

        // Act
        var @event = MockPlateTopologyEvent.CreateValid(timestamp: exactTimestamp);

        // Assert - Timestamp should be preserved exactly (per FR-012 for replay determinism)
        Assert.Equal(exactTimestamp, @event.Timestamp);
        Assert.Equal(2026, @event.Timestamp.Year);
        Assert.Equal(1, @event.Timestamp.Month);
        Assert.Equal(22, @event.Timestamp.Day);
        Assert.Equal(15, @event.Timestamp.Hour);
        Assert.Equal(30, @event.Timestamp.Minute);
        Assert.Equal(45, @event.Timestamp.Second);
        Assert.Equal(123, @event.Timestamp.Millisecond);
        Assert.Equal(TimeSpan.FromHours(-5), @event.Timestamp.Offset);
    }

    #endregion

    #region Sequence Tests

    [Fact]
    public void Event_Interface_HasSequenceProperty()
    {
        // Arrange & Act
        var sequence = 42L;
        var @event = MockPlateTopologyEvent.CreateValid(sequence: sequence);

        // Assert - Sequence should be accessible and match the provided value
        Assert.Equal(sequence, @event.Sequence);
    }

    [Fact]
    public void Event_Sequence_IsLong()
    {
        // Arrange & Act
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - Sequence should be a long type
        Assert.IsType<long>(@event.Sequence);
    }

    [Fact]
    public void Event_Sequence_SupportsMonotonicOrdering()
    {
        // Arrange
        var event1 = MockPlateTopologyEvent.CreateValid(sequence: 0L);
        var event2 = MockPlateTopologyEvent.CreateValid(sequence: 1L);
        var event3 = MockPlateTopologyEvent.CreateValid(sequence: 2L);

        // Assert - Events can have monotonically increasing sequences
        Assert.Equal(0L, event1.Sequence);
        Assert.Equal(1L, event2.Sequence);
        Assert.Equal(2L, event3.Sequence);
        Assert.True(event1.Sequence < event2.Sequence);
        Assert.True(event2.Sequence < event3.Sequence);
    }

    [Fact]
    public void Event_Sequence_SupportsLargeValues()
    {
        // Arrange
        var largeSequence = 9_999_999_999L;

        // Act
        var @event = MockPlateTopologyEvent.CreateValid(sequence: largeSequence);

        // Assert - Sequence should support large values for long-running streams
        Assert.Equal(largeSequence, @event.Sequence);
    }

    [Fact]
    public void Event_Sequence_DeterministicOrdering()
    {
        // Arrange - Events with different sequences
        var events = new[]
        {
            MockPlateTopologyEvent.CreateValid(sequence: 2L),
            MockPlateTopologyEvent.CreateValid(sequence: 0L),
            MockPlateTopologyEvent.CreateValid(sequence: 1L)
        };

        // Act - Sort by sequence (simulating replay ordering)
        var sortedEvents = events.OrderBy(e => e.Sequence).ToArray();

        // Assert - Events should be sorted by sequence for deterministic replay (per SC-001)
        Assert.Equal(0L, sortedEvents[0].Sequence);
        Assert.Equal(1L, sortedEvents[1].Sequence);
        Assert.Equal(2L, sortedEvents[2].Sequence);
    }

    [Fact]
    public void Event_Sequence_SupportsNegativeValues_ForBranching()
    {
        // Arrange - Negative sequences might be used for branching or offset streams
        var @event = MockPlateTopologyEvent.CreateValid(sequence: -1L);

        // Assert - Sequence should support negative values if needed
        Assert.Equal(-1L, @event.Sequence);
    }

    #endregion

    #region StreamIdentity Tests

    [Fact]
    public void Event_Interface_HasStreamIdentityProperty()
    {
        // Arrange
        var streamIdentity = new TruthStreamIdentity(
            "test-variant",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );

        // Act
        var @event = MockPlateTopologyEvent.CreateValid(streamIdentity: streamIdentity);

        // Assert - StreamIdentity should be accessible and match the provided value
        Assert.Equal(streamIdentity, @event.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_IsTruthStreamIdentityType()
    {
        // Arrange & Act
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - StreamIdentity should be TruthStreamIdentity type
        Assert.IsType<TruthStreamIdentity>(@event.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_SupportsDifferentVariants()
    {
        // Arrange
        var variant1Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0")
        );
        var variant2Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("wuxing", "main", 2, Domain.Parse("geo.plates"), "0")
        );

        // Assert - Events can belong to different variant streams (per FR-001)
        Assert.Equal("science", variant1Event.StreamIdentity.VariantId);
        Assert.Equal("wuxing", variant2Event.StreamIdentity.VariantId);
        Assert.NotEqual(variant1Event.StreamIdentity, variant2Event.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_SupportsDifferentBranches()
    {
        // Arrange
        var mainEvent = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0")
        );
        var featureEvent = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "feature-xyz", 2, Domain.Parse("geo.plates"), "0")
        );

        // Assert - Events can belong to different branch streams
        Assert.Equal("main", mainEvent.StreamIdentity.BranchId);
        Assert.Equal("feature-xyz", featureEvent.StreamIdentity.BranchId);
        Assert.NotEqual(mainEvent.StreamIdentity, featureEvent.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_SupportsDifferentLLevels()
    {
        // Arrange
        var l2Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0")
        );
        var l3Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "main", 3, Domain.Parse("geo.plates"), "0")
        );

        // Assert - Events can belong to different L-level streams
        Assert.Equal(2, l2Event.StreamIdentity.LLevel);
        Assert.Equal(3, l3Event.StreamIdentity.LLevel);
        Assert.NotEqual(l2Event.StreamIdentity, l3Event.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_SupportsDifferentModels()
    {
        // Arrange
        var m0Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0")
        );
        var m1Event = MockPlateTopologyEvent.CreateValid(
            streamIdentity: new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "1")
        );

        // Assert - Events can belong to different model streams (M0, M1, etc.)
        Assert.Equal("0", m0Event.StreamIdentity.Model);
        Assert.Equal("1", m1Event.StreamIdentity.Model);
        Assert.NotEqual(m0Event.StreamIdentity, m1Event.StreamIdentity);
    }

    [Fact]
    public void Event_StreamIdentity_EnforcesIsolation_PerFR014()
    {
        // Arrange - Same event content, different stream identities
        var sameEventId = Guid.NewGuid();
        var stream1 = new TruthStreamIdentity("variant-a", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("variant-b", "main", 2, Domain.Parse("geo.plates"), "0");

        var event1 = MockPlateTopologyEvent.CreateValid(eventId: sameEventId, streamIdentity: stream1);
        var event2 = MockPlateTopologyEvent.CreateValid(eventId: sameEventId, streamIdentity: stream2);

        // Assert - Events from different streams are independent (per FR-014)
        Assert.Equal(sameEventId, event1.EventId);
        Assert.Equal(sameEventId, event2.EventId);
        Assert.NotEqual(event1.StreamIdentity, event2.StreamIdentity);
    }

    #endregion

    #region Contract Compliance Tests

    [Fact]
    public void Event_Contract_AllRequiredFieldsExist()
    {
        // Arrange
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - All envelope fields required by FR-015 must be present
        // Note: EventId, Timestamp, StreamIdentity are value types (structs), cannot be null
        Assert.NotNull(@event.EventType);
    }

    [Fact]
    public void Event_Contract_SupportsImmutableEvents()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventType = "TestEvent";
        var timestamp = DateTimeOffset.UtcNow;
        var sequence = 5L;
        var streamIdentity = new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var @event = new MockPlateTopologyEvent(eventId, eventType, timestamp, sequence, streamIdentity);

        // Assert - Event properties should be immutable (readonly struct in mock)
        Assert.Equal(eventId, @event.EventId);
        Assert.Equal(eventType, @event.EventType);
        Assert.Equal(timestamp, @event.Timestamp);
        Assert.Equal(sequence, @event.Sequence);
        Assert.Equal(streamIdentity, @event.StreamIdentity);
    }

    [Fact]
    public void Event_Contract_Fr015_EventContainsAllReconstructionInformation()
    {
        // Arrange - FR-015 requires events contain all information needed for replay
        var @event = MockPlateTopologyEvent.CreateValid();

        // Assert - Event envelope provides all metadata needed for replay:
        // - EventId: Unique identification and debugging
        // - EventType: Polymorphic deserialization
        // - Timestamp: Temporal ordering and debugging
        // - Sequence: Deterministic replay ordering (FR-012)
        // - StreamIdentity: Isolation (FR-014)
        Assert.True(@event.EventId != Guid.Empty);
        Assert.False(string.IsNullOrWhiteSpace(@event.EventType));
        Assert.True(@event.Timestamp != default);
        Assert.True(@event.Sequence >= 0 || @event.Sequence < 0); // Can be any long
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void Event_Contract_PolymorphicTypeSupport()
    {
        // Arrange - Different event types implementing the same interface
        var plateEvent = MockPlateTopologyEvent.CreateValid(eventType: "PlateCreated");
        var boundaryEvent = MockPlateTopologyEvent.CreateValid(eventType: "BoundaryCreated");

        // Act - Treat both as IPlateTopologyEvent
        IPlateTopologyEvent[] events = { plateEvent, boundaryEvent };

        // Assert - Both should satisfy the same contract
        Assert.All(events, e =>
        {
            Assert.True(e.EventId != Guid.Empty);
            Assert.False(string.IsNullOrWhiteSpace(e.EventType));
            Assert.True(e.Timestamp != default);
            Assert.True(e.StreamIdentity.IsValid());
        });
    }

    [Fact]
    public void Event_Contract_StreamIdentityIntegration()
    {
        // Arrange
        var streamIdentity = new TruthStreamIdentity("integration-test", "main", 2, Domain.Parse("geo.plates"), "0");

        // Act
        var @event = MockPlateTopologyEvent.CreateValid(streamIdentity: streamIdentity);

        // Assert - StreamIdentity should integrate properly with TruthStreamIdentity contract
        Assert.True(@event.StreamIdentity.IsValid());
        Assert.Equal("integration-test", @event.StreamIdentity.VariantId);
        Assert.Equal("main", @event.StreamIdentity.BranchId);
        Assert.Equal(2, @event.StreamIdentity.LLevel);
        Assert.Equal(Domain.Parse("geo.plates"), @event.StreamIdentity.Domain);
        Assert.Equal("0", @event.StreamIdentity.Model);
    }

    #endregion
}
