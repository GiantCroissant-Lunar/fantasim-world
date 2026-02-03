using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Storage.Documents;

/// <summary>
/// Document representation of Plate for storage.
/// </summary>
[UnifyModel]
public sealed class PlateDocument
{
    public required Guid PlateId { get; init; }
    public required bool IsRetired { get; init; }
    public required string? RetirementReason { get; init; }

    /// <summary>
    /// Convert from domain entity to document.
    /// </summary>
    public static PlateDocument FromEntity(Topology.Contracts.Entities.Plate plate)
        => new()
        {
            PlateId = plate.PlateId.Value,
            IsRetired = plate.IsRetired,
            RetirementReason = plate.RetirementReason
        };

    /// <summary>
    /// Convert to domain entity.
    /// </summary>
    public Topology.Contracts.Entities.Plate ToEntity()
        => new(
            new Topology.Contracts.Entities.PlateId(PlateId),
            IsRetired,
            RetirementReason
        );
}
