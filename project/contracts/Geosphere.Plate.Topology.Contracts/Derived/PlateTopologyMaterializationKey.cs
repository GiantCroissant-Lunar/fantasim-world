using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

/// <summary>
/// Cache key for materialized topology state.
/// </summary>
/// <remarks>
/// <para>
/// Includes both the target tick and the <b>last sequence at time of materialization</b>.
/// This is critical for correctness with back-in-time events: if an event is later appended
/// with tick <= targetTick, the old cache entry becomes invalid. By including the sequence
/// number, we ensure cache misses when the event stream changes.
/// </para>
/// </remarks>
/// <param name="Stream">The truth stream identity.</param>
/// <param name="Tick">The target simulation tick.</param>
/// <param name="LastSequence">The last event sequence at time of materialization.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PlateTopologyMaterializationKey(
    TruthStreamIdentity Stream,
    long Tick,
    long LastSequence);
