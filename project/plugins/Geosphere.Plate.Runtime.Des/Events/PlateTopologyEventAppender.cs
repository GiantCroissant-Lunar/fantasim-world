using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Events;

/// <summary>
/// Appends truth events to the topology event store with optimistic concurrency control.
///
/// Design rationale (RFC-V2-0005 review):
/// - Uses <see cref="ITopologyEventStore.GetHeadAsync"/> to get current head state
/// - Passes <see cref="HeadPrecondition"/> to guard against concurrent writes
/// - Per-stream locking in the store ensures in-process atomicity
/// - <see cref="ConcurrencyConflictException"/> enables retry logic if needed
/// </summary>
public sealed class PlateTopologyEventAppender : ITruthEventAppender
{
    private readonly ITopologyEventStore _store;

    public PlateTopologyEventAppender(ITopologyEventStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<AppendDraftsResult> AppendAsync(
        IReadOnlyList<ITruthEventDraft> drafts,
        AppendOptions options,
        CancellationToken ct = default)
    {
        if (drafts.Count == 0)
        {
            // If empty, we should ideally return the current head.
            // But we don't know the stream identity if drafts is empty!
            // The drafts list carries the stream identity.
            // If the list is empty, we can't determine the stream.
            // Assuming this is a no-op that returns default/empty result.
            return new AppendDraftsResult(0, ReadOnlyMemory<byte>.Empty);
        }

        var stream = drafts[0].Stream;

        // 1. Get the current head state (sequence + hash) for optimistic concurrency
        var head = await _store.GetHeadAsync(stream, ct).ConfigureAwait(false);
        long nextSeq = head.Sequence + 1;

        var eventsToAppend = new List<IPlateTopologyEvent>(drafts.Count);

        foreach (var draft in drafts)
        {
            if (draft.Stream != stream)
            {
                 throw new ArgumentException("All drafts must belong to the same stream", nameof(drafts));
            }
            eventsToAppend.Add(draft.ToTruthEvent(nextSeq++));
        }

        // 2. Append to store with optimistic concurrency precondition
        var storeOptions = new FantaSim.Geosphere.Plate.Topology.Contracts.Events.AppendOptions
        {
            TickPolicy = options.EnforceMonotonicity
                ? TickMonotonicityPolicy.Reject
                : TickMonotonicityPolicy.Allow,
            ExpectedHead = head.ToPrecondition()
        };

        await _store.AppendAsync(stream, eventsToAppend, storeOptions, ct).ConfigureAwait(false);

        // 3. Return result from the last appended event
        // The store computes the hash but doesn't return it directly.
        // We can either:
        // a) Read back the event (current approach, slight overhead)
        // b) Have the store return the computed hash (would require API change)
        //
        // Using GetHeadAsync is more efficient than reading the full event:
        var newHead = await _store.GetHeadAsync(stream, ct).ConfigureAwait(false);

        return new AppendDraftsResult(newHead.Sequence, newHead.Hash);
    }
}
