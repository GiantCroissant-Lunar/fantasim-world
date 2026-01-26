using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public record DesRunResult(
    int ItemsProcessed,
    int EventsAppended
);

public record DesRunOptions(
    CanonicalTick StartTick,
    CanonicalTick? EndTick,
    int MaxItemsProcessed = 1000,
    int MaxEventsAppended = 1000
);

public interface IDesRuntime
{
    Task<DesRunResult> RunAsync(
        TruthStreamIdentity stream,
        DesRunOptions options,
        CancellationToken ct = default);
}
