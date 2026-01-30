using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing an update to a junction per FR-016.
///
/// Junction updates capture changes to which boundaries meet at a junction
/// and/or changes to the junction's spatial location. Per FR-016, junction
/// updates must be explicit when connected boundaries change.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sphere-by-default:</b> Location is a <see cref="SurfacePoint"/> (unit surface normal
/// + radius), not a 2D planar point. This ensures correctness anywhere on the sphere.
/// </para>
/// </remarks>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="JunctionId">The unique identifier of the junction being updated.</param>
/// <param name="NewBoundaryIds">The updated list of boundaries meeting at this junction.</param>
/// <param name="NewLocation">The new surface point location of this junction (optional).</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct JunctionUpdatedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] JunctionId JunctionId,
    [property: UnifyProperty(7)] ImmutableArray<BoundaryId> NewBoundaryIds,
    [property: UnifyProperty(8)] SurfacePoint? NewLocation,
    [property: UnifyProperty(2)] CanonicalTick Tick,
    [property: UnifyProperty(3)] long Sequence,
    [property: UnifyProperty(4)] TruthStreamIdentity StreamIdentity,
    [property: UnifyProperty(5)] ReadOnlyMemory<byte> PreviousHash,
    [property: UnifyProperty(6)] ReadOnlyMemory<byte> Hash
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(JunctionUpdatedEvent);
}
