using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

[UnifyModel]
public sealed record PlatesDatasetManifest(
    [property: UnifyProperty(0)] string DatasetId,
    [property: UnifyProperty(1)] string BodyId,
    [property: UnifyProperty(2)] BodyFrame BodyFrame,
    [property: UnifyProperty(3)] TimeMapping TimeMapping,
    [property: UnifyProperty(4)] FeatureSetAsset[] FeatureSets,
    [property: UnifyProperty(5)] RasterSequenceAsset[] RasterSequences,
    [property: UnifyProperty(6)] MotionModelAsset[] MotionModels,
    [property: UnifyProperty(7)] CanonicalizationRules CanonicalizationRules
);
