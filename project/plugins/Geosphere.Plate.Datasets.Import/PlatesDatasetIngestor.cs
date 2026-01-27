using FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Datasets.Import;

public sealed class PlatesDatasetIngestor : IPlatesDatasetIngestor
{
    private readonly IPlatesDatasetLoader _loader;
    private readonly IKinematicsEventStore? _kinematicsEventStore;

    public PlatesDatasetIngestor(IPlatesDatasetLoader loader, IKinematicsEventStore? kinematicsEventStore)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
        _kinematicsEventStore = kinematicsEventStore;
    }

    public PlatesDatasetIngestor(IKinematicsEventStore? kinematicsEventStore)
        : this(new JsonPlatesDatasetLoader(), kinematicsEventStore)
    {
    }

    public async Task<PlatesDatasetIngestResult> IngestAsync(
        string datasetRootPath,
        PlatesDatasetIngestSpec spec,
        PlatesDatasetLoadOptions? loadOptions,
        CancellationToken cancellationToken)
    {
        if (spec is null)
        {
            return new PlatesDatasetIngestResult(
                null,
                Array.Empty<TruthStreamIdentity>(),
                new[] { new DatasetValidationError("ingest_spec.required", "spec", "Ingest spec is required.") });
        }

        var loadResult = await _loader.LoadAsync(datasetRootPath, loadOptions, cancellationToken);
        if (!loadResult.IsSuccess)
        {
            return new PlatesDatasetIngestResult(
                loadResult.Dataset,
                Array.Empty<TruthStreamIdentity>(),
                loadResult.Errors);
        }

        var dataset = loadResult.Dataset!;

        if (spec.Mode == IngestMode.AssetOnly)
        {
            return new PlatesDatasetIngestResult(
                dataset,
                Array.Empty<TruthStreamIdentity>(),
                Array.Empty<DatasetValidationError>());
        }

        if (spec.Mode != IngestMode.Ingest)
        {
            return new PlatesDatasetIngestResult(
                dataset,
                Array.Empty<TruthStreamIdentity>(),
                new[] { new DatasetValidationError("ingest_mode.invalid", "mode", "Ingest mode is invalid.") });
        }

        var errors = new List<DatasetValidationError>();
        var producedStreams = new List<TruthStreamIdentity>();

        var targets = spec.Targets ?? Array.Empty<PlatesAssetIngestTarget>();
        for (var i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            var basePath = $"targets[{i}]";

            if (t is null)
            {
                errors.Add(new DatasetValidationError("ingest_target.required", basePath, "Ingest target is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(t.AssetId))
            {
                errors.Add(new DatasetValidationError("ingest_target.asset_id.required", $"{basePath}.assetId", "AssetId is required."));
                continue;
            }

            if (!t.StreamIdentity.IsValid())
            {
                errors.Add(new DatasetValidationError("ingest_target.stream.invalid", $"{basePath}.streamIdentity", "StreamIdentity is invalid."));
                continue;
            }

            var resolved = dataset.Assets.FirstOrDefault(a => a.Kind == t.Kind && string.Equals(a.AssetId, t.AssetId, StringComparison.Ordinal));
            if (resolved is null)
            {
                errors.Add(new DatasetValidationError("ingest_target.asset_missing", basePath, "Target asset was not found in the loaded dataset."));
                continue;
            }

            producedStreams.Add(t.StreamIdentity);

            switch (t.Kind)
            {
                case PlatesAssetKind.MotionModel:
                    if (t.StreamIdentity.Domain != Domain.GeoPlatesKinematics)
                    {
                        errors.Add(new DatasetValidationError(
                            "ingest_target.domain.invalid",
                            $"{basePath}.streamIdentity.domain",
                            "Motion model targets must use domain geo.plates.kinematics."));
                        continue;
                    }

                    if (_kinematicsEventStore is null)
                    {
                        errors.Add(new DatasetValidationError(
                            "ingest.kinematics_store.required",
                            "kinematicsEventStore",
                            "A kinematics event store is required for ingest."));
                        continue;
                    }

                    errors.Add(new DatasetValidationError(
                        "ingest.motion_model.not_supported",
                        basePath,
                        "Motion model ingest is not implemented yet."));
                    break;

                case PlatesAssetKind.FeatureSet:
                case PlatesAssetKind.RasterSequence:
                default:
                    errors.Add(new DatasetValidationError(
                        "ingest_target.kind.not_supported",
                        basePath,
                        "Ingest for this asset kind is not supported yet."));
                    break;
            }
        }

        producedStreams.Sort(static (a, b) => string.Compare(a.ToStreamKey(), b.ToStreamKey(), StringComparison.Ordinal));
        producedStreams = producedStreams.Distinct().ToList();

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
        {
            return new PlatesDatasetIngestResult(dataset, producedStreams, errors);
        }

        return new PlatesDatasetIngestResult(dataset, producedStreams, Array.Empty<DatasetValidationError>());
    }
}
