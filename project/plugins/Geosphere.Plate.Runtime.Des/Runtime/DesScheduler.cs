using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.World.Contracts.Time;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

/// <summary>
/// DES scheduler that assigns monotonically increasing TieBreak values
/// to ensure deterministic ordering of work items.
/// </summary>
/// <remarks>
/// The TieBreak counter is global across all (When, Sphere, Kind) tuples.
/// This guarantees that items scheduled in a specific order will always
/// be processed in that same order, even if they share the same key tuple.
/// </remarks>
public sealed class DesScheduler : IDesScheduler
{
    private readonly IDesQueue _queue;
    private ulong _tieBreakCounter;

    public DesScheduler(IDesQueue queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _tieBreakCounter = 0;
    }

    /// <inheritdoc />
    public void Schedule(CanonicalTick when, SphereId sphere, DesWorkKind kind, object? payload = null)
    {
        // Assign monotonically increasing TieBreak for deterministic ordering
        var tieBreak = _tieBreakCounter++;

        var item = new ScheduledWorkItem(when, sphere, kind, tieBreak, payload);
        _queue.Enqueue(item);
    }
}
