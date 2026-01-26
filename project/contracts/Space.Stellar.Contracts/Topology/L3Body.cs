using FantaSim.Space.Stellar.Contracts.Entities;
using FantaSim.Space.Stellar.Contracts.Mechanics;

namespace FantaSim.Space.Stellar.Contracts.Topology;

public sealed class L3Body
{
    public required Guid BodyId { get; init; }

    public required string Name { get; init; }

    public required BodyType Type { get; init; }

    /// <summary>Null for root body (star or barycenter).</summary>
    public OrbitalElements? Orbit { get; init; }

    /// <summary>Type-specific physical properties.</summary>
    public required IBodyProperties Properties { get; init; }

    /// <summary>Child bodies (moons, belts, etc.).</summary>
    public required IReadOnlyList<L3Body> Children { get; init; }

    /// <summary>Parent body ID (null for root).</summary>
    public Guid? ParentId { get; init; }

    public bool HasChildren => Children.Count != 0;

    /// <summary>Get the depth in the hierarchy (0 = root).</summary>
    public int GetDepth(L3SystemTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var depth = 0;
        var current = this;

        while (current.ParentId.HasValue)
        {
            var parent = topology.GetBodyById(current.ParentId.Value);
            if (parent is null)
                break;

            depth++;
            current = parent;
        }

        return depth;
    }
}
