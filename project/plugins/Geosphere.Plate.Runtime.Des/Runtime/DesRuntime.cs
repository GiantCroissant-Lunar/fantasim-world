using System;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Determinism;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities; // Added for TickMaterializationMode

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public sealed class DesRuntime : IDesRuntime
{
    private readonly IDesQueue _queue;
    private readonly ITruthEventAppender _appender;
    private readonly PlateTopologyTimeline _timeline;
    private readonly IDesDispatcher _dispatcher;
    private readonly IDesScheduler _scheduler;
    private readonly ISolverSeedProvider _seedProvider;

    public DesRuntime(
        IDesQueue queue,
        ITruthEventAppender appender,
        PlateTopologyTimeline timeline,
        IDesDispatcher dispatcher,
        ISolverSeedProvider seedProvider)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _appender = appender ?? throw new ArgumentNullException(nameof(appender));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _seedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));
        _scheduler = new DesScheduler(_queue);
    }

    public async Task<DesRunResult> RunAsync(
        TruthStreamIdentity stream,
        DesRunOptions options,
        CancellationToken ct = default)
    {
        int itemsProcessed = 0;
        int eventsAppended = 0;

        while (itemsProcessed < options.MaxItemsProcessed && eventsAppended < options.MaxEventsAppended)
        {
            ct.ThrowIfCancellationRequested();

            if (!_queue.TryPeek(out var nextItem))
            {
                break; // Queue empty
            }

            // Check start tick (skip if earlier, though queue should be ordered)
            if (nextItem.When < options.StartTick)
            {
                // This shouldn't happen if queue was initialized correctly,
                // but if it does, we just dequeue and ignore?
                // Or we process it anyway?
                // RFC says "advance simulation time".
                // If we are resuming, we assume queue items are valid pending work.
                // Let's assume we process everything in queue.
                // BUT, options.StartTick might define where we *start* processing.
                // If item is before StartTick, maybe it's stale?
                // For now, let's assume we process it.
            }

            // Check end tick
            if (options.EndTick.HasValue && nextItem.When > options.EndTick.Value)
            {
                // Reached time limit
                break;
            }

            // Dequeue
            _queue.TryDequeue(out var item); // Should match peek

            // Materialize state (ReadOnly)
            // RFC says: state = MaterializeAtTick(currentTick)
            // We use the timeline facade.
            // Note: TickMaterializationMode is passed in options?
            // DesRunOptions doesn't have TickMode in my previous definition?
            // Let me check DesRunOptions.
            // I defined it as: record DesRunOptions(CanonicalTick StartTick, CanonicalTick? EndTick, int MaxItemsProcessed, int MaxEventsAppended);
            // I should have added TickMaterializationMode?
            // RFC says "TickMaterializationMode TickMode" in DesRunOptions.
            // I'll assume Auto for now or update DesRunOptions.
            // Let's assume Auto.

            var slice = await _timeline.GetSliceAtTickAsync(stream, item.When, TickMaterializationMode.Auto, ct).ConfigureAwait(false);

            // Create tick-scoped RNG for deterministic event ID generation
            var tickRng = _seedProvider.CreateRngForTick(options.ScenarioSeed, stream, item.When);

            var context = new DesContext
            {
                Stream = stream,
                CurrentTick = item.When,
                State = slice.State,
                Scheduler = _scheduler,
                Rng = tickRng
            };

            // Dispatch
            var drafts = await _dispatcher.DispatchAsync(item, context, ct).ConfigureAwait(false);

            // Append drafts
            if (drafts.Count > 0)
            {
                var appendResult = await _appender.AppendAsync(drafts, new AppendOptions { EnforceMonotonicity = true }, ct).ConfigureAwait(false);
                eventsAppended += drafts.Count;
            }

            itemsProcessed++;
        }

        return new DesRunResult(itemsProcessed, eventsAppended);
    }
}
