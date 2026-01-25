using FantaSim.Geosphere.Plate.Runtime.Des.Core;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public interface IDesScheduler
{
    void Schedule(ScheduledWorkItem item);
}
