using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;

public interface IPlatesDatasetIngestor
{
    Task<PlatesDatasetIngestResult> IngestAsync(
        string datasetRootPath,
        PlatesDatasetIngestSpec spec,
        PlatesDatasetLoadOptions? loadOptions,
        CancellationToken cancellationToken);
}
