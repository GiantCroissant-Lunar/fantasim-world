using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

[UnifyModel]
public sealed record FeatureSetAsset(
    [property: UnifyProperty(0)] string AssetId,
    [property: UnifyProperty(1)] string RelativePath,
    [property: UnifyProperty(2)] string Format
);
