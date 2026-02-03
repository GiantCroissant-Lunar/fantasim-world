using System.IO;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Canonicalization;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Import;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Tests.Ingest;

public sealed class DatasetIngestorTests
{
    [Fact]
    public async Task IngestAsync_ModeAssetOnly_LoadsDatasetAndDoesNotWriteTruth()
    {
        var store = new RecordingKinematicsEventStore();
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

        var datasetRoot = GetSampleRoot("ValidSphere");
        var spec = new PlatesDatasetIngestSpec(IngestMode.AssetOnly, Array.Empty<PlatesAssetIngestTarget>());

        var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Dataset.Should().NotBeNull();
        r.Audit.Should().NotBeNull();
        r.ProducedStreams.Should().BeEmpty();
        store.AppendCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task IngestAsync_ModeAssetOnly_AuditIsDeterministicAcrossDifferentRoots()
    {
        var rootA = CopySampleToTemp("ValidSphere");
        var rootB = CopySampleToTemp("ValidSphere");

        var store = new RecordingKinematicsEventStore();
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

        var spec = new PlatesDatasetIngestSpec(IngestMode.AssetOnly, Array.Empty<PlatesAssetIngestTarget>());

        var a = await ingestor.IngestAsync(rootA, spec, null, CancellationToken.None);
        var b = await ingestor.IngestAsync(rootB, spec, null, CancellationToken.None);

        a.IsSuccess.Should().BeTrue();
        b.IsSuccess.Should().BeTrue();

        a.Audit.Should().NotBeNull();
        b.Audit.Should().NotBeNull();

        a.Audit!.AuditSha256.Should().Be(b.Audit!.AuditSha256);
        a.Audit.ManifestCanonicalSha256.Should().Be(b.Audit.ManifestCanonicalSha256);
        a.Audit.ManifestFileSha256.Should().Be(b.Audit.ManifestFileSha256);
    }

    [Fact]
    public async Task IngestAsync_ModeIngest_SegmentsV1MotionModel_MaterializesRotation()
    {
        var kv = new InMemoryOrderedKeyValueStore();
        var store = new PlateKinematicsEventStore(kv);
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

        var datasetRoot = GetSampleRoot("MotionModelSegmentsV1");
        var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesKinematics, "M0");

        var spec = new PlatesDatasetIngestSpec(
            IngestMode.Ingest,
            new[] { new PlatesAssetIngestTarget(PlatesAssetKind.MotionModel, "mm1", stream) });

        var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Audit.Should().NotBeNull();
        (await store.GetLastSequenceAsync(stream, CancellationToken.None)).Should().NotBeNull();

        var plateId = new PlateId(DeterministicIdPolicy.DeriveStableId("test.dataset.motion", "mm1", "plate", "p1"));

        var materializer = new PlateKinematicsMaterializer(store);
        var state = await materializer.MaterializeAsync(stream);

        state.TryGetRotation(plateId, new CanonicalTick(5), out var rotAt5).Should().BeTrue();
        rotAt5.Should().NotBe(Quaterniond.Identity);
    }

    [Fact]
    public async Task IngestAsync_ModeIngest_AuditIsDeterministicAcrossDifferentRoots()
    {
        static async Task<PlatesDatasetIngestAudit> RunOnceAsync(string datasetRoot)
        {
            var kv = new InMemoryOrderedKeyValueStore();
            var store = new PlateKinematicsEventStore(kv);
            var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

            var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesKinematics, "M0");
            var spec = new PlatesDatasetIngestSpec(
                IngestMode.Ingest,
                new[] { new PlatesAssetIngestTarget(PlatesAssetKind.MotionModel, "mm1", stream) });

            var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);
            r.IsSuccess.Should().BeTrue();
            r.Audit.Should().NotBeNull();
            return r.Audit!;
        }

        var rootA = CopySampleToTemp("MotionModelSegmentsV1");
        var rootB = CopySampleToTemp("MotionModelSegmentsV1");

        var a = await RunOnceAsync(rootA);
        var b = await RunOnceAsync(rootB);

        a.AuditSha256.Should().Be(b.AuditSha256);
        a.ManifestCanonicalSha256.Should().Be(b.ManifestCanonicalSha256);
        a.ManifestFileSha256.Should().Be(b.ManifestFileSha256);
        a.Assets.Should().Equal(b.Assets);
        a.Targets.Should().Equal(b.Targets);
        a.Streams.Should().Equal(b.Streams);
    }

    [Fact]
    public async Task IngestAsync_ModeIngest_SegmentsV1MotionModel_PersistedBytesAreDeterministic()
    {
        static async Task<List<(byte[] Key, byte[] Value)>> RunOnceAsync()
        {
            var kv = new InMemoryOrderedKeyValueStore();
            var store = new PlateKinematicsEventStore(kv);
            var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

            var datasetRoot = GetSampleRoot("MotionModelSegmentsV1");
            var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesKinematics, "M0");
            var spec = new PlatesDatasetIngestSpec(
                IngestMode.Ingest,
                new[] { new PlatesAssetIngestTarget(PlatesAssetKind.MotionModel, "mm1", stream) });

            var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);
            r.IsSuccess.Should().BeTrue();

            return DumpAll(kv);
        }

        var a = await RunOnceAsync();
        var b = await RunOnceAsync();

        a.Count.Should().Be(b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            a[i].Key.Should().Equal(b[i].Key);
            a[i].Value.Should().Equal(b[i].Value);
        }
    }

    [Fact]
    public async Task IngestAsync_ModeIngest_FeatureSetTarget_FailsDeterministically()
    {
        var store = new RecordingKinematicsEventStore();
        var kv = new InMemoryOrderedKeyValueStore();
        using var topoStore = new PlateTopologyEventStore(kv);
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), topoStore, store);

        var datasetRoot = GetSampleRoot("ValidSphere");
        var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesTopology, "M0");
        var spec = new PlatesDatasetIngestSpec(
            IngestMode.Ingest,
            new[] { new PlatesAssetIngestTarget(PlatesAssetKind.FeatureSet, "a", stream) });

        var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        r.Errors[0].Code.Should().Be("topology.format.unsupported");
        store.AppendCalls.Should().BeEmpty();
        (await topoStore.GetLastSequenceAsync(stream, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task IngestAsync_ModeIngest_TopologyV1FeatureSet_MaterializesTopology()
    {
        var kv = new InMemoryOrderedKeyValueStore();
        using var topoStore = new PlateTopologyEventStore(kv);
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), topoStore, kinematicsEventStore: null);

        var datasetRoot = GetSampleRoot("TopologyV1");
        var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesTopology, "M0");

        var spec = new PlatesDatasetIngestSpec(
            IngestMode.Ingest,
            new[] { new PlatesAssetIngestTarget(PlatesAssetKind.FeatureSet, "topo1", stream) });

        var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);
        r.IsSuccess.Should().BeTrue();

        var materializer = new PlateTopologyMaterializer(topoStore);
        var state = await materializer.MaterializeAsync(stream, CancellationToken.None);

        state.Plates.Count.Should().Be(2);
        state.Boundaries.Count.Should().Be(1);
        state.Junctions.Count.Should().Be(0);

        // Verify boundary geometry is Polyline3 (not Polyline2)
        var boundary = state.Boundaries.Values.First();
        boundary.Geometry.Should().BeOfType<Polyline3>("because topology-v1 ingest should convert lon/lat to unit-sphere Polyline3");
    }

    private static string GetSampleRoot(string folderName)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "SampleData", folderName);
    }

    private static List<(byte[] Key, byte[] Value)> DumpAll(IKeyValueStore store)
    {
        var list = new List<(byte[] Key, byte[] Value)>();
        using var it = store.CreateIterator();
        it.Seek(new byte[] { 0 });
        while (it.Valid)
        {
            list.Add((it.Key.ToArray(), it.Value.ToArray()));
            it.Next();
        }
        return list;
    }

    private static string CopySampleToTemp(string folderName)
    {
        var source = GetSampleRoot(folderName);

        var target = Path.Combine(Path.GetTempPath(), $"fantasim-world-dataset-{Guid.NewGuid():N}");
        Directory.CreateDirectory(target);

        CopyDirectory(source, target);
        return target;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            File.Copy(filePath, Path.Combine(targetDir, name), overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(subDir);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            CopyDirectory(subDir, Path.Combine(targetDir, name));
        }
    }

    private sealed class RecordingKinematicsEventStore : IKinematicsEventStore
    {
        public List<(TruthStreamIdentity Stream, List<IPlateKinematicsEvent> Events)> AppendCalls { get; } = new();

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateKinematicsEvent> events, CancellationToken cancellationToken)
        {
            return AppendAsync(stream, events, FantaSim.Geosphere.Plate.Kinematics.Contracts.Events.AppendOptions.Default, cancellationToken);
        }

        public Task AppendAsync(
            TruthStreamIdentity stream,
            IEnumerable<IPlateKinematicsEvent> events,
            FantaSim.Geosphere.Plate.Kinematics.Contracts.Events.AppendOptions options,
            CancellationToken cancellationToken)
        {
            AppendCalls.Add((stream, events.ToList()));
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<IPlateKinematicsEvent> ReadAsync(TruthStreamIdentity stream, long fromSequenceInclusive, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield break;
        }

        public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
        {
            return Task.FromResult<long?>(null);
        }
    }
}
