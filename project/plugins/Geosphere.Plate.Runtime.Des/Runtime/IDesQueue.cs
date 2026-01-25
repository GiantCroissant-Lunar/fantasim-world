using FantaSim.Geosphere.Plate.Runtime.Des.Core;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public interface IDesQueue
{
    void Enqueue(ScheduledWorkItem item);
    bool TryDequeue(out ScheduledWorkItem item);
    int Count { get; }
    bool TryPeek(out ScheduledWorkItem item);
}
