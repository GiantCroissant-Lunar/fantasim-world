using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

[UnifyModel]
public sealed record CanonicalizationRules(
    [property: UnifyProperty(0)] int Version,
    [property: UnifyProperty(1)] string StableIdPolicyId,
    [property: UnifyProperty(2)] string AssetOrderingPolicyId,
    [property: UnifyProperty(3)] string QuantizationPolicyId
);
