using System.Collections.Generic;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public sealed class PriorityQueueDesQueue : IDesQueue
{
    private readonly PriorityQueue<ScheduledWorkItem, ScheduledWorkItem> _queue;

    public PriorityQueueDesQueue()
    {
        // ScheduledWorkItem implements IComparable<ScheduledWorkItem>
        // using the canonical ordering key defined in RFC-V2-0012
        _queue = new PriorityQueue<ScheduledWorkItem, ScheduledWorkItem>(Comparer<ScheduledWorkItem>.Default);
    }

    public int Count => _queue.Count;

    public void Enqueue(ScheduledWorkItem item)
    {
        _queue.Enqueue(item, item);
    }

    public bool TryDequeue(out ScheduledWorkItem item)
    {
        return _queue.TryDequeue(out item, out _);
    }

    public bool TryPeek(out ScheduledWorkItem item)
    {
        return _queue.TryPeek(out item, out _);
    }
}
