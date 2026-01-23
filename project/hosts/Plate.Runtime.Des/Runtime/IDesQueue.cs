using Plate.Runtime.Des.Core;

namespace Plate.Runtime.Des.Runtime;

public interface IDesQueue
{
    void Enqueue(ScheduledWorkItem item);
    bool TryDequeue(out ScheduledWorkItem item);
    int Count { get; }
    bool TryPeek(out ScheduledWorkItem item);
}
