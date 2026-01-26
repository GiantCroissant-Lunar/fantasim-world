using System.Collections.Immutable;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Snapshots;

[UnifyModel]
public readonly record struct L3SystemSnapshot(
    [property: UnifyProperty(0)] Guid SystemId,
    [property: UnifyProperty(1)] ImmutableArray<L3BodySnapshot> Bodies,
    [property: UnifyProperty(2)] double CurrentTimeS
)
{
    public L3BodySnapshot? GetBody(Guid bodyId)
    {
        foreach (var body in Bodies)
        {
            if (body.BodyId == bodyId)
                return body;
        }

        return null;
    }
}
