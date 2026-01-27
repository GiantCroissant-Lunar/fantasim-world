using System.IO;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;

public static class PlatesDatasetManifestValidator
{
    public static IReadOnlyList<DatasetValidationError> Validate(PlatesDatasetManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors = new List<DatasetValidationError>();

        if (string.IsNullOrWhiteSpace(manifest.DatasetId))
            errors.Add(new DatasetValidationError("dataset_id.required", "datasetId", "DatasetId is required."));

        if (string.IsNullOrWhiteSpace(manifest.BodyId))
            errors.Add(new DatasetValidationError("body_id.required", "bodyId", "BodyId is required."));

        if (manifest.BodyFrame is null)
        {
            errors.Add(new DatasetValidationError("body_frame.required", "bodyFrame", "BodyFrame is required."));
        }
        else
        {
            ValidateBodyFrame(manifest.BodyFrame, errors);
        }

        if (manifest.TimeMapping is null)
        {
            errors.Add(new DatasetValidationError("time_mapping.required", "timeMapping", "TimeMapping is required."));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(manifest.TimeMapping.TickUnit))
            {
                errors.Add(new DatasetValidationError("tick_unit.required", "timeMapping.tickUnit", "TickUnit is required."));
            }
            else if (!string.Equals(manifest.TimeMapping.TickUnit, "CTU", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new DatasetValidationError("tick_unit.invalid", "timeMapping.tickUnit", "TickUnit must be CTU."));
            }
        }

        ValidateAssets(manifest, errors);

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

        return errors;
    }

    private static void ValidateBodyFrame(BodyFrame bodyFrame, List<DatasetValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(bodyFrame.Unit))
            errors.Add(new DatasetValidationError("body_frame.unit.required", "bodyFrame.unit", "BodyFrame.Unit is required."));

        if (string.IsNullOrWhiteSpace(bodyFrame.AngularConvention))
            errors.Add(new DatasetValidationError("body_frame.angular_convention.required", "bodyFrame.angularConvention", "BodyFrame.AngularConvention is required."));

        switch (bodyFrame.Shape)
        {
            case BodyShape.Sphere:
                if (!bodyFrame.Radius.HasValue || bodyFrame.Radius.Value <= 0)
                    errors.Add(new DatasetValidationError("body_frame.radius.required", "bodyFrame.radius", "BodyFrame.Radius must be > 0 for spheres."));
                if (bodyFrame.SemiMajor.HasValue)
                    errors.Add(new DatasetValidationError("body_frame.semi_major.unexpected", "bodyFrame.semiMajor", "SemiMajor must be null for spheres."));
                if (bodyFrame.SemiMinor.HasValue)
                    errors.Add(new DatasetValidationError("body_frame.semi_minor.unexpected", "bodyFrame.semiMinor", "SemiMinor must be null for spheres."));
                break;

            case BodyShape.Ellipsoid:
                if (!bodyFrame.SemiMajor.HasValue || bodyFrame.SemiMajor.Value <= 0)
                    errors.Add(new DatasetValidationError("body_frame.semi_major.required", "bodyFrame.semiMajor", "SemiMajor must be > 0 for ellipsoids."));
                if (!bodyFrame.SemiMinor.HasValue || bodyFrame.SemiMinor.Value <= 0)
                    errors.Add(new DatasetValidationError("body_frame.semi_minor.required", "bodyFrame.semiMinor", "SemiMinor must be > 0 for ellipsoids."));
                if (bodyFrame.Radius.HasValue)
                    errors.Add(new DatasetValidationError("body_frame.radius.unexpected", "bodyFrame.radius", "Radius must be null for ellipsoids."));
                break;

            default:
                errors.Add(new DatasetValidationError("body_frame.shape.invalid", "bodyFrame.shape", "BodyFrame.Shape is invalid."));
                break;
        }
    }

    private static void ValidateAssets(PlatesDatasetManifest manifest, List<DatasetValidationError> errors)
    {
        var featureSets = manifest.FeatureSets ?? Array.Empty<FeatureSetAsset>();
        var rasterSequences = manifest.RasterSequences ?? Array.Empty<RasterSequenceAsset>();
        var motionModels = manifest.MotionModels ?? Array.Empty<MotionModelAsset>();

        var assetIds = new Dictionary<string, int>(StringComparer.Ordinal);

        ValidateAssetIds(featureSets.Select(a => (a.AssetId, "featureSets")), assetIds, errors);
        ValidateAssetIds(rasterSequences.Select(a => (a.AssetId, "rasterSequences")), assetIds, errors);
        ValidateAssetIds(motionModels.Select(a => (a.AssetId, "motionModels")), assetIds, errors);

        ValidateAssetPaths(featureSets.Select(a => (a.RelativePath, "featureSets")), errors);
        ValidateAssetPaths(rasterSequences.Select(a => (a.RelativePath, "rasterSequences")), errors);
        ValidateAssetPaths(motionModels.Select(a => (a.RelativePath, "motionModels")), errors);

        ValidateAssetFormats(featureSets.Select(a => (a.Format, "featureSets")), errors);
        ValidateAssetFormats(rasterSequences.Select(a => (a.Format, "rasterSequences")), errors);
        ValidateAssetFormats(motionModels.Select(a => (a.Format, "motionModels")), errors);
    }

    private static void ValidateAssetIds(IEnumerable<(string AssetId, string Group)> ids, Dictionary<string, int> seen, List<DatasetValidationError> errors)
    {
        var index = 0;
        foreach (var (assetId, group) in ids)
        {
            var path = $"{group}[{index}].assetId";
            if (string.IsNullOrWhiteSpace(assetId))
            {
                errors.Add(new DatasetValidationError("asset_id.required", path, "AssetId is required."));
            }
            else if (!seen.TryAdd(assetId, 1))
            {
                errors.Add(new DatasetValidationError("asset_id.duplicate", path, "AssetId must be unique across all assets."));
            }

            index++;
        }
    }

    private static void ValidateAssetPaths(IEnumerable<(string RelativePath, string Group)> paths, List<DatasetValidationError> errors)
    {
        var index = 0;
        foreach (var (relativePath, group) in paths)
        {
            var path = $"{group}[{index}].relativePath";
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                errors.Add(new DatasetValidationError("asset_path.required", path, "RelativePath is required."));
            }
            else if (Path.IsPathRooted(relativePath))
            {
                errors.Add(new DatasetValidationError("asset_path.rooted", path, "RelativePath must be a relative path."));
            }

            index++;
        }
    }

    private static void ValidateAssetFormats(IEnumerable<(string Format, string Group)> formats, List<DatasetValidationError> errors)
    {
        var index = 0;
        foreach (var (format, group) in formats)
        {
            var path = $"{group}[{index}].format";
            if (string.IsNullOrWhiteSpace(format))
            {
                errors.Add(new DatasetValidationError("asset_format.required", path, "Format is required."));
            }

            index++;
        }
    }
}
