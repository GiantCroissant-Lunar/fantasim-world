using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

namespace FantaSim.Geosphere.Plate.Datasets.Import;

internal sealed class PlatesDataset : IPlatesDataset
{
    public PlatesDataset(string datasetRootPath, PlatesDatasetManifest manifest, IReadOnlyList<ResolvedAsset> assets)
    {
        DatasetRootPath = datasetRootPath;
        Manifest = manifest;
        Assets = assets;
    }

    public string DatasetRootPath { get; }

    public PlatesDatasetManifest Manifest { get; }

    public IReadOnlyList<ResolvedAsset> Assets { get; }
}
