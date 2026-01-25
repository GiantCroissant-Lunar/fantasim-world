using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public interface IDesDispatcher
{
    Task<IReadOnlyList<ITruthEventDraft>> DispatchAsync(
        ScheduledWorkItem item,
        DesContext context,
        CancellationToken ct);
}
