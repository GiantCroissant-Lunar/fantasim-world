using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.World.Contracts.Time;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

/// <summary>
/// Scheduler interface for the DES runtime.
/// </summary>
/// <remarks>
/// The scheduler is responsible for assigning monotonically increasing TieBreak values
/// to ensure deterministic ordering when multiple work items share the same
/// (When, Sphere, Kind) tuple. Callers should NOT provide TieBreak values directly.
/// </remarks>
public interface IDesScheduler
{
    /// <summary>
    /// Schedules a work item for execution at the specified tick.
    /// The scheduler assigns a deterministic TieBreak value automatically.
    /// </summary>
    /// <param name="when">The tick at which to execute the work.</param>
    /// <param name="sphere">The sphere responsible for this work.</param>
    /// <param name="kind">The type of work to perform.</param>
    /// <param name="payload">Optional payload data for the work item.</param>
    void Schedule(CanonicalTick when, SphereId sphere, DesWorkKind kind, object? payload = null);
}
