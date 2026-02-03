using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Context;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Reconstruction.Query;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

public sealed class QueryServiceSmokeTests
{
    [Fact]
    public void Reconstruct_EmptyStreams_ReturnsEmptyFeatures_AndZeroHashes()
    {
        using var kv = new InMemoryOrderedKeyValueStore();

        var topoStore = new PlateTopologyEventStore(kv);
        var kinStore = new PlateKinematicsEventStore(kv);

        var topologyTimeline = new PlateTopologyTimeline(topoStore, topoStore);
        var kinematicsMaterializer = new PlateKinematicsMaterializer(kinStore);

        var selection = new FixedPlatesTruthStreamSelection(
            Topology: new TruthStreamIdentity("science", "trunk", 2, Domain.Parse("geo.plates"), "0"),
            Kinematics: new TruthStreamIdentity("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0"));

        var sut = new PlateReconstructionQueryService(
            selection,
            topologyTimeline,
            kinematicsMaterializer,
            topoStore,
            kinStore,
            new NaivePlateReconstructionSolver());

        var policy = new ReconstructionPolicy
        {
            Frame = MantleFrame.Instance,
            KinematicsModel = ModelId.NewId(),
            PartitionTolerance = new FantaSim.Geosphere.Plate.Partition.Contracts.TolerancePolicy.StrictPolicy()
        };

        var result = sut.Reconstruct(FeatureSetId.NewId(), CanonicalTick.Genesis, policy);

        result.Features.Should().BeEmpty();

        result.Metadata.TopologyStreamHash.Should().HaveLength(64);
        result.Metadata.TopologyStreamHash.Should().Be(new string('0', 64));

        result.Metadata.KinematicsStreamHash.Should().HaveLength(64);
        result.Metadata.KinematicsStreamHash.Should().Be(new string('0', 64));
    }

    private sealed record FixedPlatesTruthStreamSelection(
        TruthStreamIdentity Topology,
        TruthStreamIdentity Kinematics) : IPlatesTruthStreamSelection
    {
        public PlatesTruthStreamSelection GetCurrent() => new(Topology, Kinematics);
    }
}
