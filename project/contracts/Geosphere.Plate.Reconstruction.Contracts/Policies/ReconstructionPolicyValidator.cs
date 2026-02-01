namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

public static class ReconstructionPolicyValidator
{
    public static ValidationResult ValidateForQuery(ReconstructionPolicy policy, QueryType queryType)
    {
        var errors = new List<string>();

        if (policy.Frame is null &&
            queryType is QueryType.Reconstruct or QueryType.QueryVelocity or QueryType.BoundaryAnalytics or QueryType.MotionPath or QueryType.Flowline)
        {
            errors.Add("Frame is required.");
        }

        // RFC-V2-0050 §4.1: KinematicsModel and PartitionTolerance are required for all query types.
        if (policy.KinematicsModel.IsEmpty)
            errors.Add("KinematicsModel is required.");

        if (policy.PartitionTolerance == null)
            errors.Add("PartitionTolerance is required.");

        // Query-specific checks
        switch (queryType)
        {
            case QueryType.Reconstruct:
                // Required: Frame, KinematicsModel, PartitionTolerance
                break;

            case QueryType.QueryPlateId:
                // Required: KinematicsModel, PartitionTolerance
                // Optional: Frame
                break;

            case QueryType.QueryVelocity:
                // Required: Frame, KinematicsModel, PartitionTolerance
                break;

            case QueryType.BoundaryAnalytics:
                // Required: Frame, KinematicsModel, PartitionTolerance, BoundarySampling
                if (policy.BoundarySampling == null)
                    errors.Add("BoundarySampling is required for BoundaryAnalytics queries.");
                break;

            case QueryType.MotionPath:
            case QueryType.Flowline:
                // Required: Frame, KinematicsModel, PartitionTolerance, IntegrationPolicy
                if (policy.IntegrationPolicy == null)
                    errors.Add("IntegrationPolicy is required for MotionPath/Flowline queries.");
                break;
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

public readonly record struct ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
