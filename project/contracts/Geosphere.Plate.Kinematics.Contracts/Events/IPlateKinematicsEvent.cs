using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;

/// <summary>
/// Base interface for all plate kinematics truth events (RFC-V2-0023).
/// </summary>
public interface IPlateKinematicsEvent
{
    Guid EventId { get; }

    string EventType { get; }

    CanonicalTick Tick { get; }

    long Sequence { get; }

    TruthStreamIdentity StreamIdentity { get; }

    ReadOnlyMemory<byte> PreviousHash { get; }

    ReadOnlyMemory<byte> Hash { get; }
}
