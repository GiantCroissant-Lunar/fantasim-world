using System.IO;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Canonicalization;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Import;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
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
        r.ProducedStreams.Should().BeEmpty();
        store.AppendCalls.Should().BeEmpty();
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
        (await store.GetLastSequenceAsync(stream, CancellationToken.None)).Should().NotBeNull();

        var plateId = new PlateId(DeterministicIdPolicy.DeriveStableId("test.dataset.motion", "mm1", "plate", "p1"));

        var materializer = new PlateKinematicsMaterializer(store);
        var state = await materializer.MaterializeAsync(stream);

        state.TryGetRotation(plateId, new CanonicalTick(5), out var rotAt5).Should().BeTrue();
        rotAt5.Should().NotBe(Quaterniond.Identity);
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
        var ingestor = new PlatesDatasetIngestor(new JsonPlatesDatasetLoader(), store);

        var datasetRoot = GetSampleRoot("ValidSphere");
        var stream = new TruthStreamIdentity("main", "trunk", 0, Domain.GeoPlatesTopology, "M0");
        var spec = new PlatesDatasetIngestSpec(
            IngestMode.Ingest,
            new[] { new PlatesAssetIngestTarget(PlatesAssetKind.FeatureSet, "a", stream) });

        var r = await ingestor.IngestAsync(datasetRoot, spec, null, CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        r.Errors[0].Code.Should().Be("ingest_target.kind.not_supported");
        store.AppendCalls.Should().BeEmpty();
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

    private sealed class RecordingKinematicsEventStore : IKinematicsEventStore
    {
        public List<(TruthStreamIdentity Stream, List<IPlateKinematicsEvent> Events)> AppendCalls { get; } = new();

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateKinematicsEvent> events, CancellationToken cancellationToken)
        {
            return AppendAsync(stream, events, AppendOptions.Default, cancellationToken);
        }

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateKinematicsEvent> events, AppendOptions options, CancellationToken cancellationToken)
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
