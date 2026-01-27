using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

public interface IPlatesDataset
{
    string DatasetRootPath { get; }

    PlatesDatasetManifest Manifest { get; }

    IReadOnlyList<ResolvedAsset> Assets { get; }
}
