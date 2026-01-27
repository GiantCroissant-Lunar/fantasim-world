namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;

public sealed record PlatesDatasetLoadOptions(
    bool ValidateOnly = false,
    string ManifestFileName = "plates.dataset.json");
