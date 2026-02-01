namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

/// <summary>
/// Identifies the type of reconstruction query for validation purposes.
/// </summary>
public enum QueryType
{
    Reconstruct,
    QueryPlateId,
    QueryVelocity,
    BoundaryAnalytics,
    MotionPath,
    Flowline
}
