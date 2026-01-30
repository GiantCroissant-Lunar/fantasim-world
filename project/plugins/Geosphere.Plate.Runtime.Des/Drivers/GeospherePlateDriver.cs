using System.Threading;
using System.Threading.Tasks;
using FantaSim.World.Contracts.Time;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Materializer;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Drivers;

public sealed class GeospherePlateDriver : IDriver
{
    public DriverId Id => new("GeospherePlateDriver");
    public SphereId Sphere => SphereIds.Geosphere;

    public Task<DriverOutput> EvaluateAsync(DesContext context, CancellationToken ct = default)
    {
        // 1. Read state
        var plateCount = context.State.Plates.Count;
        object? signal = null;

        if (plateCount == 0)
        {
            // Genesis logic: Create initial plate
            signal = "Genesis";
        }
        else
        {
            // Normal simulation step
            // For MVP, just log or no-op
            signal = "Step";
        }

        // 2. Schedule next step
        // Schedule RunPlateSolver 10 ticks later
        // Note: In strict DES, we might only schedule if there's active work.
        // For this MVP, we keep the heartbeat alive.
        var nextTick = context.CurrentTick + 10;

        // Scheduler assigns monotonic TieBreak automatically for deterministic ordering
        context.Scheduler.Schedule(nextTick, SphereIds.Geosphere, DesWorkKind.RunPlateSolver);

        return Task.FromResult(new DriverOutput(signal));
    }
}
