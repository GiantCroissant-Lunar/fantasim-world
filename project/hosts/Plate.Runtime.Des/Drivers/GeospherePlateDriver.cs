using System.Threading;
using System.Threading.Tasks;
using FantaSim.World.Contracts.Time;
using Plate.Runtime.Des.Core;
using Plate.Runtime.Des.Runtime;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Materializer;

namespace Plate.Runtime.Des.Drivers;

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

        context.Scheduler.Schedule(new ScheduledWorkItem(
            nextTick,
            SphereIds.Geosphere,
            DesWorkKind.RunPlateSolver,
            0, // TieBreak
            null
        ));

        return Task.FromResult(new DriverOutput(signal));
    }
}
