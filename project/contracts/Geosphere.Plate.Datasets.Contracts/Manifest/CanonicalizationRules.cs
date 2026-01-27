using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

[UnifyModel]
public sealed record CanonicalizationRules(
    [property: UnifyProperty(0)] int Version
);
