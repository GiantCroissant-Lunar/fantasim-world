using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;

public sealed record PlatesDatasetIngestResult(
    IPlatesDataset? Dataset,
    IReadOnlyList<TruthStreamIdentity> ProducedStreams,
    IReadOnlyList<DatasetValidationError> Errors)
{
    public bool IsSuccess => Dataset is not null && Errors.Count == 0;
}
