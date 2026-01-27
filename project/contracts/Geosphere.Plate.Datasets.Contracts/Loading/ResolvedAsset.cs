using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

[UnifyModel]
public sealed record ResolvedAsset(
    [property: UnifyProperty(0)] PlatesAssetKind Kind,
    [property: UnifyProperty(1)] string AssetId,
    [property: UnifyProperty(2)] string AbsolutePath,
    [property: UnifyProperty(3)] string Format
);
