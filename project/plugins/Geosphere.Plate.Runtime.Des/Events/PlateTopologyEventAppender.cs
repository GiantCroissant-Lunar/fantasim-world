using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Events;

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

        // 1. Get the last sequence number to determine next sequence
        var lastSeq = await _store.GetLastSequenceAsync(stream, ct);
        long nextSeq = (lastSeq ?? -1) + 1;

        var eventsToAppend = new List<IPlateTopologyEvent>(drafts.Count);

        foreach (var draft in drafts)
        {
            if (draft.Stream != stream)
            {
                 throw new ArgumentException("All drafts must belong to the same stream", nameof(drafts));
            }
            eventsToAppend.Add(draft.ToTruthEvent(nextSeq++));
        }

        // 2. Append to store
        var storeOptions = new FantaSim.Geosphere.Plate.Topology.Contracts.Events.AppendOptions
        {
            TickPolicy = options.EnforceMonotonicity
                ? TickMonotonicityPolicy.Reject
                : TickMonotonicityPolicy.Allow
        };

        await _store.AppendAsync(stream, eventsToAppend, storeOptions, ct);

        // 3. Return result from the last appended event
        // Note: The store computes the Hash. IPlateTopologyEvent has a Hash property.
        // Wait, the IPlateTopologyEvent passed to AppendAsync usually has empty Hash/PreviousHash,
        // and the store computes it?
        // Checking PlateTopologyEventStore implementation...
        // It computes hash: var hash = MessagePackEventRecordSerializer.ComputeHashV1(...)
        // But it does NOT update the 'evt' object in the list (records represent persistence).
        // The persistence layer stores the hash.
        // If we want the computed hash back, we might need to re-read the head, or the Store should return it.
        // The current ITopologyEventStore.AppendAsync returns Task (void).
        // So we must read the head to get the hash.

        // Optimization: The store updates the Head key. We can read it.
        // But GetLastSequenceAsync only returns the sequence number.
        // To get the Hash, we need to read the event at that sequence.

        // Since we know the sequence of the last event we just appended:
        long finalSeq = nextSeq - 1;

        // Read back the last event to get its computed hash.
        // This is a slight overhead but ensures we return the authoritative hash from the store.
        var lastEvents = _store.ReadAsync(stream, finalSeq, ct);
        IPlateTopologyEvent? lastEvent = null;
        await foreach (var evt in lastEvents)
        {
            lastEvent = evt;
            break; // Should only be one
        }

        if (lastEvent == null)
        {
             throw new InvalidOperationException($"Failed to read back event at sequence {finalSeq} after append.");
        }

        return new AppendDraftsResult(lastEvent.Sequence, lastEvent.Hash);
    }
}
