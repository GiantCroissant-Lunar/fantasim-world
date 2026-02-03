using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Provenance;

/// <summary>
/// Builder for constructing <see cref="ProvenanceChain"/> instances with required fields.
/// Supports incremental building per RFC-V2-0045 section 5.1 provenance requirements.
/// </summary>
public sealed class ProvenanceBuilder
{
    private FeatureId[] _sourceFeatureIds = [];
    private BoundaryId[] _sourceBoundaryIds = [];
    private JunctionId[] _sourceJunctionIds = [];
    private PlateAssignmentProvenance? _plateAssignment;
    private KinematicsProvenance? _kinematics;
    private StreamProvenance? _stream;
    private QueryProvenanceMetadata? _queryMetadata;

    /// <summary>
    /// Creates a new provenance builder instance.
    /// </summary>
    public static ProvenanceBuilder Create() => new();

    /// <summary>
    /// Sets the source feature identifiers.
    /// </summary>
    public ProvenanceBuilder WithSourceFeatureIds(params FeatureId[] featureIds)
    {
        _sourceFeatureIds = featureIds;
        return this;
    }

    /// <summary>
    /// Sets the source boundary identifiers.
    /// </summary>
    public ProvenanceBuilder WithSourceBoundaryIds(params BoundaryId[] boundaryIds)
    {
        _sourceBoundaryIds = boundaryIds;
        return this;
    }

    /// <summary>
    /// Sets the source junction identifiers.
    /// </summary>
    public ProvenanceBuilder WithSourceJunctionIds(params JunctionId[] junctionIds)
    {
        _sourceJunctionIds = junctionIds;
        return this;
    }

    /// <summary>
    /// Sets the plate assignment provenance.
    /// </summary>
    public ProvenanceBuilder WithPlateAssignment(
        PlateId plateId,
        PlateAssignmentMethod method,
        double confidence)
    {
        _plateAssignment = new PlateAssignmentProvenance
        {
            AssignedPlateId = plateId,
            Method = method,
            Confidence = confidence
        };
        return this;
    }

    /// <summary>
    /// Sets the plate assignment provenance from an existing instance.
    /// </summary>
    public ProvenanceBuilder WithPlateAssignment(PlateAssignmentProvenance provenance)
    {
        _plateAssignment = provenance;
        return this;
    }

    /// <summary>
    /// Sets the kinematics provenance.
    /// </summary>
    public ProvenanceBuilder WithKinematics(
        ReferenceFrameId referenceFrame,
        Guid[] motionSegmentIds,
        string interpolationMethod)
    {
        _kinematics = new KinematicsProvenance
        {
            ReferenceFrame = referenceFrame,
            MotionSegmentIds = motionSegmentIds,
            InterpolationMethod = interpolationMethod
        };
        return this;
    }

    /// <summary>
    /// Sets the kinematics provenance from an existing instance.
    /// </summary>
    public ProvenanceBuilder WithKinematics(KinematicsProvenance provenance)
    {
        _kinematics = provenance;
        return this;
    }

    /// <summary>
    /// Sets the stream provenance with topology and kinematics hashes.
    /// </summary>
    public ProvenanceBuilder WithStreamHashes(
        string topologyStreamHash,
        string kinematicsStreamHash,
        CanonicalTick topologyReferenceTick,
        CanonicalTick kinematicsReferenceTick)
    {
        _stream = new StreamProvenance
        {
            TopologyStreamHash = topologyStreamHash,
            KinematicsStreamHash = kinematicsStreamHash,
            TopologyReferenceTick = topologyReferenceTick,
            KinematicsReferenceTick = kinematicsReferenceTick
        };
        return this;
    }

    /// <summary>
    /// Sets the stream provenance from an existing instance.
    /// </summary>
    public ProvenanceBuilder WithStream(StreamProvenance provenance)
    {
        _stream = provenance;
        return this;
    }

    /// <summary>
    /// Sets the query metadata.
    /// </summary>
    public ProvenanceBuilder WithQueryMetadata(
        CanonicalTick queryTick,
        string solverVersion)
    {
        _queryMetadata = new QueryProvenanceMetadata
        {
            QueryTick = queryTick,
            ExecutionTimestampUtc = DateTimeOffset.UtcNow.Ticks,
            SolverVersion = solverVersion
        };
        return this;
    }

    /// <summary>
    /// Sets the query metadata with explicit execution timestamp.
    /// </summary>
    public ProvenanceBuilder WithQueryMetadata(
        CanonicalTick queryTick,
        long executionTimestampUtc,
        string solverVersion)
    {
        _queryMetadata = new QueryProvenanceMetadata
        {
            QueryTick = queryTick,
            ExecutionTimestampUtc = executionTimestampUtc,
            SolverVersion = solverVersion
        };
        return this;
    }

    /// <summary>
    /// Sets the query metadata from an existing instance.
    /// </summary>
    public ProvenanceBuilder WithQueryMetadata(QueryProvenanceMetadata metadata)
    {
        _queryMetadata = metadata;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ProvenanceChain"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required provenance components are not set.
    /// </exception>
    public ProvenanceChain Build()
    {
        if (_plateAssignment is null)
            throw new InvalidOperationException("PlateAssignment provenance is required.");

        if (_kinematics is null)
            throw new InvalidOperationException("Kinematics provenance is required.");

        if (_stream is null)
            throw new InvalidOperationException("Stream provenance is required.");

        if (_queryMetadata is null)
            throw new InvalidOperationException("QueryMetadata is required.");

        return new ProvenanceChain
        {
            SourceFeatureIds = _sourceFeatureIds,
            SourceBoundaryIds = _sourceBoundaryIds,
            SourceJunctionIds = _sourceJunctionIds,
            PlateAssignment = _plateAssignment,
            Kinematics = _kinematics,
            Stream = _stream,
            QueryMetadata = _queryMetadata
        };
    }
}
