using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;

namespace FantaSim.Geosphere.Plate.Datasets.Import;

public sealed class JsonPlatesDatasetLoader : IPlatesDatasetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<PlatesDatasetLoadResult> LoadAsync(
        string datasetRootPath,
        PlatesDatasetLoadOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new PlatesDatasetLoadOptions();

        if (string.IsNullOrWhiteSpace(datasetRootPath))
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("dataset_root.required", "datasetRootPath", "Dataset root path is required.") });
        }

        if (!Directory.Exists(datasetRootPath))
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("dataset_root.missing", "datasetRootPath", "Dataset root path does not exist.") });
        }

        var manifestPath = Path.Combine(datasetRootPath, options.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("manifest.missing", "manifest", "Dataset manifest file does not exist.") });
        }

        PlatesDatasetManifest? manifest;
        try
        {
            await using var stream = File.OpenRead(manifestPath);
            manifest = await JsonSerializer.DeserializeAsync<PlatesDatasetManifest>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("manifest.json.invalid", "manifest", "Dataset manifest JSON is invalid.") });
        }
        catch (IOException)
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("manifest.read_failed", "manifest", "Failed to read dataset manifest file.") });
        }

        if (manifest is null)
        {
            return new PlatesDatasetLoadResult(
                null,
                new[] { new DatasetValidationError("manifest.json.null", "manifest", "Dataset manifest JSON deserialized to null.") });
        }

        var errors = PlatesDatasetManifestValidator.Validate(manifest).ToList();

        var assets = new List<ResolvedAsset>();

        BuildAssets(datasetRootPath, "featureSets", manifest.FeatureSets, PlatesAssetKind.FeatureSet, options.ValidateOnly, assets, errors);
        BuildAssets(datasetRootPath, "rasterSequences", manifest.RasterSequences, PlatesAssetKind.RasterSequence, options.ValidateOnly, assets, errors);
        BuildAssets(datasetRootPath, "motionModels", manifest.MotionModels, PlatesAssetKind.MotionModel, options.ValidateOnly, assets, errors);

        errors.Sort(static (a, b) =>
        {
            var c = string.Compare(a.Code, b.Code, StringComparison.Ordinal);
            if (c != 0)
                return c;

            c = string.Compare(a.Path, b.Path, StringComparison.Ordinal);
            if (c != 0)
                return c;

            return string.Compare(a.Message, b.Message, StringComparison.Ordinal);
        });

        if (errors.Count != 0)
            return new PlatesDatasetLoadResult(null, errors);

        assets.Sort(static (a, b) =>
        {
            var c = a.Kind.CompareTo(b.Kind);
            if (c != 0)
                return c;

            c = string.Compare(a.AssetId, b.AssetId, StringComparison.Ordinal);
            if (c != 0)
                return c;

            return string.Compare(a.AbsolutePath, b.AbsolutePath, StringComparison.Ordinal);
        });

        return new PlatesDatasetLoadResult(
            new PlatesDataset(datasetRootPath, manifest, assets),
            Array.Empty<DatasetValidationError>());
    }

    private static void BuildAssets<TAsset>(
        string datasetRootPath,
        string group,
        TAsset[]? rawAssets,
        PlatesAssetKind kind,
        bool validateOnly,
        List<ResolvedAsset> resolved,
        List<DatasetValidationError> errors)
        where TAsset : class
    {
        var assets = rawAssets ?? Array.Empty<TAsset>();

        for (var i = 0; i < assets.Length; i++)
        {
            var asset = assets[i];

            string assetId;
            string relativePath;
            string format;

            switch (asset)
            {
                case FeatureSetAsset fs:
                    assetId = fs.AssetId;
                    relativePath = fs.RelativePath;
                    format = fs.Format;
                    break;
                case RasterSequenceAsset rs:
                    assetId = rs.AssetId;
                    relativePath = rs.RelativePath;
                    format = rs.Format;
                    break;
                case MotionModelAsset mm:
                    assetId = mm.AssetId;
                    relativePath = mm.RelativePath;
                    format = mm.Format;
                    break;
                default:
                    continue;
            }

            if (!PathResolver.TryResolveFile(datasetRootPath, relativePath, out var absolutePath))
            {
                errors.Add(new DatasetValidationError("asset_path.invalid", $"{group}[{i}].relativePath", "Asset path is invalid."));
                continue;
            }

            if (!validateOnly && !File.Exists(absolutePath))
            {
                errors.Add(new DatasetValidationError("asset_file.missing", $"{group}[{i}].relativePath", "Referenced asset file does not exist."));
                continue;
            }

            resolved.Add(new ResolvedAsset(kind, assetId, absolutePath, format));
        }
    }
}
