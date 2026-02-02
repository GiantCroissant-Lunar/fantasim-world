using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Events;

public interface ITruthEventAppender
{
    Task<AppendDraftsResult> AppendAsync(
        IReadOnlyList<ITruthEventDraft> drafts,
        AppendOptions options,
        CancellationToken ct = default);
}
