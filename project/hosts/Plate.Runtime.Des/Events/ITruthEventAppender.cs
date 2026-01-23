using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Plate.Runtime.Des.Events;

public record AppendDraftsResult(
    long LastSequence,
    ReadOnlyMemory<byte> LastHash
);

public record AppendOptions(
    bool EnforceMonotonicity = true
);

public interface ITruthEventAppender
{
    Task<AppendDraftsResult> AppendAsync(
        IReadOnlyList<ITruthEventDraft> drafts,
        AppendOptions options,
        CancellationToken ct = default);
}
