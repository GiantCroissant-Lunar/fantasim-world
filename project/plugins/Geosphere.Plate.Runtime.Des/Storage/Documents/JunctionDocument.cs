using System.Collections.Immutable;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Storage.Documents;

/// <summary>
/// Document representation of Junction for storage.
/// </summary>
[UnifyModel]
public sealed class JunctionDocument
{
    public required Guid JunctionId { get; init; }
    public required List<Guid> BoundaryIds { get; init; } = [];
    public required double NormalX { get; init; }
    public required double NormalY { get; init; }
    public required double NormalZ { get; init; }
    public required double Radius { get; init; }
    public required bool IsRetired { get; init; }
    public required string? RetirementReason { get; init; }

    /// <summary>
    /// Convert from domain entity to document.
    /// </summary>
    public static JunctionDocument FromEntity(Topology.Contracts.Entities.Junction junction)
    {
        var location = junction.Location;
        return new()
        {
            JunctionId = junction.JunctionId.Value,
            BoundaryIds = junction.BoundaryIds.Select(b => b.Value).ToList(),
            NormalX = location.Normal.X,
            NormalY = location.Normal.Y,
            NormalZ = location.Normal.Z,
            Radius = location.Radius,
            IsRetired = junction.IsRetired,
            RetirementReason = junction.RetirementReason
        };
    }

    /// <summary>
    /// Convert to domain entity.
    /// </summary>
    public Topology.Contracts.Entities.Junction ToEntity()
    {
        var normal = new Topology.Contracts.Numerics.UnitVector3d(NormalX, NormalY, NormalZ);
        var surfacePoint = new Topology.Contracts.Numerics.SurfacePoint(normal, Radius);

        return new Topology.Contracts.Entities.Junction(
            new Topology.Contracts.Entities.JunctionId(JunctionId),
            BoundaryIds.Select(b => new Topology.Contracts.Entities.BoundaryId(b)).ToImmutableArray(),
            surfacePoint,
            IsRetired,
            RetirementReason
        );
    }
}
