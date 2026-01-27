using System.IO;
using System.Text.Json;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Canonicalization;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

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

        var perStreamPlans = new Dictionary<TruthStreamIdentity, List<KinematicsEventPlan>>(new TruthStreamIdentityEqualityComparer());

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

                    if (!string.Equals(resolved.Format, "segments-v1", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new DatasetValidationError(
                            "motion_model.format.unsupported",
                            basePath,
                            "Motion model format is not supported for ingest."));
                        continue;
                    }

                    var motionModelErrors = new List<DatasetValidationError>();
                    var plans = await SegmentsV1MotionModelParser.BuildKinematicsPlansAsync(
                        resolved.AbsolutePath,
                        dataset.Manifest.DatasetId,
                        resolved.AssetId,
                        t.StreamIdentity,
                        motionModelErrors,
                        cancellationToken);

                    if (motionModelErrors.Count != 0)
                    {
                        errors.AddRange(motionModelErrors.Select(e => e with { Path = $"{basePath}.{e.Path}" }));
                        continue;
                    }

                    if (!perStreamPlans.TryGetValue(t.StreamIdentity, out var streamPlans))
                    {
                        streamPlans = new List<KinematicsEventPlan>();
                        perStreamPlans.Add(t.StreamIdentity, streamPlans);
                    }

                    streamPlans.AddRange(plans);
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

        if (_kinematicsEventStore is null)
        {
            return new PlatesDatasetIngestResult(
                dataset,
                producedStreams,
                new[] { new DatasetValidationError("ingest.kinematics_store.required", "kinematicsEventStore", "A kinematics event store is required for ingest.") });
        }

        var orderedStreams = perStreamPlans.Keys
            .OrderBy(s => s.ToStreamKey(), StringComparer.Ordinal)
            .ToList();

        foreach (var stream in orderedStreams)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var plans = perStreamPlans[stream];
            plans.Sort(KinematicsEventPlan.Compare);

            var lastSeq = await _kinematicsEventStore.GetLastSequenceAsync(stream, cancellationToken);
            var nextSeq = lastSeq.HasValue ? lastSeq.Value + 1 : 0;

            var events = new List<IPlateKinematicsEvent>(plans.Count);
            for (var i = 0; i < plans.Count; i++)
            {
                var seq = nextSeq + i;
                var evtId = DeterministicIdPolicy.DeriveEventId(dataset.Manifest.DatasetId, plans[i].AssetId, seq);
                events.Add(plans[i].Create(evtId, seq));
            }

            await _kinematicsEventStore.AppendAsync(stream, events, cancellationToken);
        }

        return new PlatesDatasetIngestResult(dataset, producedStreams, Array.Empty<DatasetValidationError>());
    }

    private sealed class TruthStreamIdentityEqualityComparer : IEqualityComparer<TruthStreamIdentity>
    {
        public bool Equals(TruthStreamIdentity x, TruthStreamIdentity y) => x.Equals(y);

        public int GetHashCode(TruthStreamIdentity obj) => obj.GetHashCode();
    }

    private sealed record KinematicsEventPlan(
        string AssetId,
        int KindOrder,
        string OrderKey,
        Func<Guid, long, IPlateKinematicsEvent> Create)
    {
        public static int Compare(KinematicsEventPlan a, KinematicsEventPlan b)
        {
            var c = a.KindOrder.CompareTo(b.KindOrder);
            if (c != 0)
                return c;

            c = string.Compare(a.OrderKey, b.OrderKey, StringComparison.Ordinal);
            if (c != 0)
                return c;

            return string.Compare(a.AssetId, b.AssetId, StringComparison.Ordinal);
        }
    }

    private static class SegmentsV1MotionModelParser
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<IReadOnlyList<KinematicsEventPlan>> BuildKinematicsPlansAsync(
            string motionModelPath,
            string datasetId,
            string assetId,
            TruthStreamIdentity stream,
            List<DatasetValidationError> errors,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (string.IsNullOrWhiteSpace(motionModelPath))
            {
                errors.Add(new DatasetValidationError("motion_model.path.required", "motionModel.path", "Motion model path is required."));
                return Array.Empty<KinematicsEventPlan>();
            }

            if (!File.Exists(motionModelPath))
            {
                errors.Add(new DatasetValidationError("motion_model.file.missing", "motionModel.path", "Motion model file does not exist."));
                return Array.Empty<KinematicsEventPlan>();
            }

            MotionModelSegmentsV1? doc;
            try
            {
                await using var streamIn = File.OpenRead(motionModelPath);
                doc = await JsonSerializer.DeserializeAsync<MotionModelSegmentsV1>(streamIn, JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                errors.Add(new DatasetValidationError("motion_model.json.invalid", "motionModel", "Motion model JSON is invalid."));
                return Array.Empty<KinematicsEventPlan>();
            }
            catch (IOException)
            {
                errors.Add(new DatasetValidationError("motion_model.read_failed", "motionModel", "Failed to read motion model file."));
                return Array.Empty<KinematicsEventPlan>();
            }

            if (doc is null)
            {
                errors.Add(new DatasetValidationError("motion_model.json.null", "motionModel", "Motion model JSON deserialized to null."));
                return Array.Empty<KinematicsEventPlan>();
            }

            if (!string.Equals(doc.Schema, "segments-v1", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new DatasetValidationError("motion_model.schema.invalid", "motionModel.schema", "Motion model schema must be segments-v1."));
            }

            if (string.IsNullOrWhiteSpace(doc.ModelId))
            {
                errors.Add(new DatasetValidationError("motion_model.model_id.required", "motionModel.modelId", "ModelId is required."));
            }

            var plates = doc.Plates ?? Array.Empty<MotionModelPlateV1>();
            var plateKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < plates.Length; i++)
            {
                var p = plates[i];
                var platePath = $"motionModel.plates[{i}]";

                if (p is null)
                {
                    errors.Add(new DatasetValidationError("motion_model.plate.required", platePath, "Plate entry is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(p.PlateKey))
                {
                    errors.Add(new DatasetValidationError("motion_model.plate_key.required", $"{platePath}.plateKey", "PlateKey is required."));
                    continue;
                }

                if (!plateKeys.Add(p.PlateKey))
                {
                    errors.Add(new DatasetValidationError("motion_model.plate_key.duplicate", $"{platePath}.plateKey", "PlateKey must be unique."));
                }

                var segments = p.Segments ?? Array.Empty<MotionModelSegmentV1>();
                for (var j = 0; j < segments.Length; j++)
                {
                    var s = segments[j];
                    var segPath = $"{platePath}.segments[{j}]";
                    if (s is null)
                    {
                        errors.Add(new DatasetValidationError("motion_model.segment.required", segPath, "Segment entry is required."));
                        continue;
                    }

                    if (s.TickA < 0)
                        errors.Add(new DatasetValidationError("motion_model.segment.tick_a.invalid", $"{segPath}.tickA", "TickA must be non-negative."));

                    if (s.TickB < 0)
                        errors.Add(new DatasetValidationError("motion_model.segment.tick_b.invalid", $"{segPath}.tickB", "TickB must be non-negative."));

                    if (s.TickB <= s.TickA)
                        errors.Add(new DatasetValidationError("motion_model.segment.tick_range.invalid", segPath, "Segment must satisfy tickB > tickA."));
                }
            }

            if (errors.Count != 0)
            {
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
                return Array.Empty<KinematicsEventPlan>();
            }

            var plans = new List<KinematicsEventPlan>();

            var orderedPlates = plates
                .Where(p => p is not null)
                .OrderBy(p => p!.PlateKey, StringComparer.Ordinal)
                .ToList();

            foreach (var plate in orderedPlates)
            {
                var plateKey = plate!.PlateKey;

                var plateGuid = TryParseGuid(plate.PlateId)
                    ?? DeterministicIdPolicy.DeriveStableId(datasetId, assetId, "plate", plateKey);

                var plateId = new PlateId(plateGuid);

                var segments = (plate.Segments ?? Array.Empty<MotionModelSegmentV1>())
                    .Where(s => s is not null)
                    .OrderBy(s => s!.TickA)
                    .ThenBy(s => s!.TickB)
                    .ThenBy(s => s!.AxisAzimuthMicroDeg)
                    .ThenBy(s => s!.AxisElevationMicroDeg)
                    .ThenBy(s => s!.AngleMicroDeg)
                    .ToList();

                var assignTick = segments.Count != 0 ? segments[0]!.TickA : 0;

                plans.Add(new KinematicsEventPlan(
                    assetId,
                    KindOrder: 0,
                    OrderKey: $"plate:{plateKey}",
                    Create: (eventId, sequence) => new PlateMotionModelAssignedEvent(
                        eventId,
                        plateId,
                        doc.ModelId,
                        new CanonicalTick(assignTick),
                        sequence,
                        stream,
                        ReadOnlyMemory<byte>.Empty,
                        ReadOnlyMemory<byte>.Empty)));

                foreach (var seg in segments)
                {
                    var tickA = new CanonicalTick(seg!.TickA);
                    var tickB = new CanonicalTick(seg.TickB);

                    var segmentKey = $"{plateKey}:{seg.TickA}:{seg.TickB}:{seg.AxisAzimuthMicroDeg}:{seg.AxisElevationMicroDeg}:{seg.AngleMicroDeg}";
                    var segGuid = TryParseGuid(seg.SegmentId)
                        ?? DeterministicIdPolicy.DeriveStableId(datasetId, assetId, "segment", segmentKey);
                    var segmentId = new MotionSegmentId(segGuid);

                    var rot = QuantizedEulerPoleRotation.Create(seg.AxisAzimuthMicroDeg, seg.AxisElevationMicroDeg, seg.AngleMicroDeg);

                    plans.Add(new KinematicsEventPlan(
                        assetId,
                        KindOrder: 1,
                        OrderKey: $"seg:{plateKey}:{seg.TickA:D20}:{seg.TickB:D20}:{seg.AxisAzimuthMicroDeg:D11}:{seg.AxisElevationMicroDeg:D11}:{seg.AngleMicroDeg:D11}",
                        Create: (eventId, sequence) => new MotionSegmentUpsertedEvent(
                            eventId,
                            plateId,
                            segmentId,
                            tickA,
                            tickB,
                            rot,
                            tickA,
                            sequence,
                            stream,
                            ReadOnlyMemory<byte>.Empty,
                            ReadOnlyMemory<byte>.Empty)));
                }
            }

            plans.Sort(KinematicsEventPlan.Compare);
            return plans;
        }

        private static Guid? TryParseGuid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return Guid.TryParse(value, out var g) ? g : null;
        }

        private sealed record MotionModelSegmentsV1(
            string Schema,
            string ModelId,
            MotionModelPlateV1[] Plates);

        private sealed record MotionModelPlateV1(
            string PlateKey,
            string? PlateId,
            MotionModelSegmentV1[] Segments);

        private sealed record MotionModelSegmentV1(
            long TickA,
            long TickB,
            int AxisAzimuthMicroDeg,
            int AxisElevationMicroDeg,
            int AngleMicroDeg,
            string? SegmentId);
    }
}
