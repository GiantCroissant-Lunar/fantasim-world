using Plate.Runtime.Des.Core;

namespace Plate.Runtime.Des.Runtime;

public sealed class DesScheduler : IDesScheduler
{
    private readonly IDesQueue _queue;

    public DesScheduler(IDesQueue queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public void Schedule(ScheduledWorkItem item)
    {
        _queue.Enqueue(item);
    }
}
