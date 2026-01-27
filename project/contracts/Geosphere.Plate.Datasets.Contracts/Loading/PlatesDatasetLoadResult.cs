using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

public sealed record PlatesDatasetLoadResult(
    IPlatesDataset? Dataset,
    IReadOnlyList<DatasetValidationError> Errors)
{
    public bool IsSuccess => Dataset is not null && Errors.Count == 0;
}
