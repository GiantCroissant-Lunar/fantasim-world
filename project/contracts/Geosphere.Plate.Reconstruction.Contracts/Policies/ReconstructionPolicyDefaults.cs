using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FrameId = FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

public static class ReconstructionPolicyDefaults
{
    public static ReconstructionPolicy Standard(ModelId model) => new()
    {
        Frame = MantleFrame.Instance,
        KinematicsModel = model,
        PartitionTolerance = new TolerancePolicy.StrictPolicy(),
        Strictness = ProvenanceStrictness.Strict
    };

    public static ReconstructionPolicy Visualization(ModelId model) => new()
    {
        Frame = MantleFrame.Instance,
        KinematicsModel = model,
        PartitionTolerance = new TolerancePolicy.LenientPolicy(0.001),
        Strictness = ProvenanceStrictness.Lenient
    };

    /// <summary>
    /// MVP preset for motion path queries.
    /// Matches the legacy RFC-V2-0035 "fixed steps / no adaptive" constraints.
    /// </summary>
    /// <param name="stepTicks">Fixed step size in canonical ticks</param>
    /// <returns>Policy configured for MVP motion path computation</returns>
    public static ReconstructionPolicy MvpMotionPathPreset(CanonicalTick stepTicks) => new()
    {
        Frame = MantleFrame.Instance,
        KinematicsModel = ModelId.Default,
        PartitionTolerance = new TolerancePolicy.PolygonizerDefaultPolicy(),
        BoundarySampling = null,
        IntegrationPolicy = new StepPolicy.FixedInterval(stepTicks.Value),
        Strictness = ProvenanceStrictness.Strict
    };
}
