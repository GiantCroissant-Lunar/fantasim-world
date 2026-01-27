using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;

[UnifyModel]
public sealed record PlatesAssetIngestTarget(
    [property: UnifyProperty(0)] PlatesAssetKind Kind,
    [property: UnifyProperty(1)] string AssetId,
    [property: UnifyProperty(2)] TruthStreamIdentity StreamIdentity
);
