using FantaSim.Space.Stellar.Contracts.Entities;

namespace FantaSim.Space.Stellar.Contracts.Topology;

public sealed class L3SystemTopology
{
    public required Guid SystemId { get; init; }

    public required string SystemName { get; init; }

    public required L3Body RootBody { get; init; }

    public required double EpochTimeS { get; init; }

    private Dictionary<Guid, L3Body>? _bodyIndex;

    public L3Body? GetBodyById(Guid bodyId)
    {
        _bodyIndex ??= BuildIndex(RootBody);
        return _bodyIndex.GetValueOrDefault(bodyId);
    }

    public IEnumerable<L3Body> GetAllBodies()
    {
        _bodyIndex ??= BuildIndex(RootBody);
        return _bodyIndex.Values;
    }

    public IEnumerable<L3Body> GetBodiesOfType(BodyType type)
        => GetAllBodies().Where(b => b.Type == type);

    private static Dictionary<Guid, L3Body> BuildIndex(L3Body root)
    {
        var index = new Dictionary<Guid, L3Body>();

        void Traverse(L3Body body)
        {
            index[body.BodyId] = body;
            foreach (var child in body.Children)
                Traverse(child);
        }

        Traverse(root);
        return index;
    }
}
