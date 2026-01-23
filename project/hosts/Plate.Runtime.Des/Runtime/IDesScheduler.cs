using Plate.Runtime.Des.Core;

namespace Plate.Runtime.Des.Runtime;

public interface IDesScheduler
{
    void Schedule(ScheduledWorkItem item);
}
