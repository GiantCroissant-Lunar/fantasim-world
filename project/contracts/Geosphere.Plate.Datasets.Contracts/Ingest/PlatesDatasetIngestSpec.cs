using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;

[UnifyModel]
public sealed record PlatesDatasetIngestSpec(
    [property: UnifyProperty(0)] IngestMode Mode,
    [property: UnifyProperty(1)] PlatesAssetIngestTarget[] Targets
);
