using System.IO;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Import;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FluentAssertions;

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
