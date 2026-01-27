namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

public interface IPlatesDatasetLoader
{
    Task<PlatesDatasetLoadResult> LoadAsync(
        string datasetRootPath,
        PlatesDatasetLoadOptions? options,
        CancellationToken cancellationToken);
}
