using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public record DesRunResult(
    int ItemsProcessed,
    int EventsAppended
);

/// <summary>
/// Options for running the DES simulation loop.
/// </summary>
/// <param name="StartTick">The tick to start simulation from.</param>
/// <param name="EndTick">Optional tick to end simulation at.</param>
/// <param name="ScenarioSeed">The scenario seed for deterministic RNG derivation.
/// Required for reproducible event ID generation.</param>
/// <param name="MaxItemsProcessed">Maximum number of work items to process.</param>
/// <param name="MaxEventsAppended">Maximum number of events to append.</param>
public record DesRunOptions(
    CanonicalTick StartTick,
    CanonicalTick? EndTick,
    ulong ScenarioSeed,
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
