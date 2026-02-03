using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Cache;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Context;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Provenance;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Query;

public sealed class PlateReconstructionQueryService : IPlateReconstructionQueryService
{
    private readonly IPlatesTruthStreamSelection _selection;
    private readonly PlateTopologyTimeline _topologyTimeline;
    private readonly PlateKinematicsMaterializer _kinematicsMaterializer;
    private readonly ITopologyEventStore _topologyEventStore;
    private readonly IKinematicsEventStore _kinematicsEventStore;
    private readonly IPlateReconstructionSolver _solver;

    public PlateReconstructionQueryService(
        IPlatesTruthStreamSelection selection,
        PlateTopologyTimeline topologyTimeline,
        PlateKinematicsMaterializer kinematicsMaterializer,
        ITopologyEventStore topologyEventStore,
        IKinematicsEventStore kinematicsEventStore,
        IPlateReconstructionSolver solver)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _topologyTimeline = topologyTimeline ?? throw new ArgumentNullException(nameof(topologyTimeline));
        _kinematicsMaterializer = kinematicsMaterializer ?? throw new ArgumentNullException(nameof(kinematicsMaterializer));
        _topologyEventStore = topologyEventStore ?? throw new ArgumentNullException(nameof(topologyEventStore));
        _kinematicsEventStore = kinematicsEventStore ?? throw new ArgumentNullException(nameof(kinematicsEventStore));
        _solver = solver ?? throw new ArgumentNullException(nameof(solver));
    }

    public ReconstructResult Reconstruct(
        FeatureSetId featureSetId,
        CanonicalTick targetTick,
        ReconstructionPolicy policy)
    {
        var validation = ReconstructionPolicyValidator.ValidateForQuery(policy, QueryType.Reconstruct);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                $"Invalid ReconstructionPolicy for Reconstruct: {string.Join("; ", validation.Errors)}",
                nameof(policy));
        }

        var streams = _selection.GetCurrent();

        var topoHead = _topologyEventStore
            .GetHeadAsync(streams.TopologyStream, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var topologyStreamHash = Convert.ToHexString(topoHead.Hash);
        var kinematicsStreamHash = GetKinematicsStreamHashHex(streams.KinematicsStream);

        var topoSlice = _topologyTimeline
            .GetSliceAtTickAsync(streams.TopologyStream, targetTick, cancellationToken: CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var kinematics = _kinematicsMaterializer
            .MaterializeAsync(streams.KinematicsStream, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var boundaries = _solver.ReconstructBoundaries(
            topoSlice.State,
            kinematics,
            policy,
            targetTick);

        var features = boundaries
            .Select(b => new ReconstructedFeature(
                new FeatureId(b.BoundaryId.Value),
                b.PlateIdProvenance,
                b.Geometry))
            .OrderBy(f => f.FeatureId.Value)
            .ToArray();

        var provenance = ProvenanceBuilder.Create()
            .WithSourceBoundaryIds(boundaries.Select(b => b.BoundaryId).ToArray())
            .WithPlateAssignment(
                new PlateId(Guid.Empty),
                PlateAssignmentMethod.Explicit,
                confidence: 1.0)
            .WithKinematics(
                policy.Frame,
                Array.Empty<Guid>(),
                interpolationMethod: "identity-fallback")
            .WithStreamHashes(
                topologyStreamHash,
                kinematicsStreamHash,
                targetTick,
                targetTick)
            // Keep deterministic for now; hosts can provide wall-clock timestamps separately.
            .WithQueryMetadata(
                targetTick,
                executionTimestampUtc: 0,
                solverVersion: GetSolverVersion())
            .Build();

        var cacheKey = $"{featureSetId.Value:D}:{PolicyCacheKey.ComputeCacheKey(targetTick, policy, topologyStreamHash, kinematicsStreamHash)}";

        var metadata = new QueryMetadata
        {
            QueryContractVersion = "RFC-V2-0045",
            SolverImplementation = _solver.GetType().Name,
            CacheHit = false,
            CacheKey = cacheKey,
            TopologyStreamHash = topologyStreamHash,
            KinematicsStreamHash = kinematicsStreamHash,
            TopologyReferenceTick = targetTick,
            QueryTick = targetTick,
            Warnings = Array.Empty<string>()
        };

        return new ReconstructResult
        {
            Features = features,
            Provenance = provenance,
            Metadata = metadata
        };
    }

    public PlateAssignmentResult QueryPlateId(Point3 point, CanonicalTick tick, ReconstructionPolicy policy)
    {
        throw new NotSupportedException("QueryPlateId is not implemented yet (requires Partition integration).");
    }

    public VelocityResult QueryVelocity(Point3 point, CanonicalTick tick, ReconstructionPolicy policy)
    {
        throw new NotSupportedException("QueryVelocity is not implemented yet (requires Velocity integration).");
    }

    private static string GetSolverVersion()
    {
        var asmVersion = typeof(PlateReconstructionQueryService).Assembly.GetName().Version?.ToString();
        return asmVersion is null ? "unknown" : $"Geosphere.Plate.Reconstruction.Query@{asmVersion}";
    }

    private string GetKinematicsStreamHashHex(FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentity stream)
    {
        var lastSeq = _kinematicsEventStore
            .GetLastSequenceAsync(stream, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        if (!lastSeq.HasValue)
        {
            return Convert.ToHexString(new byte[FantaSim.Geosphere.Plate.Topology.Contracts.Events.StreamHead.HashSizeBytes]);
        }

        var enumerator = _kinematicsEventStore
            .ReadAsync(stream, lastSeq.Value, CancellationToken.None)
            .GetAsyncEnumerator(CancellationToken.None);

        try
        {
            if (!enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                return Convert.ToHexString(new byte[FantaSim.Geosphere.Plate.Topology.Contracts.Events.StreamHead.HashSizeBytes]);

            return Convert.ToHexString(enumerator.Current.Hash.ToArray());
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
