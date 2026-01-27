using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

[UnifyModel]
public sealed record TimeMapping(
    [property: UnifyProperty(0)] string TickUnit
);
