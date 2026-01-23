using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Plate.Runtime.Des.Core;
using Plate.Runtime.Des.Events;

namespace Plate.Runtime.Des.Runtime;

public interface IDesDispatcher
{
    Task<IReadOnlyList<ITruthEventDraft>> DispatchAsync(
        ScheduledWorkItem item,
        DesContext context,
        CancellationToken ct);
}
