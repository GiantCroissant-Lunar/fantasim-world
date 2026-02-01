using MessagePack;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Configuration for motion path integration steps (RFC-V2-0049 §3.3).
/// </summary>
[MessagePackObject]
[Union(0, typeof(Adaptive))]
[Union(1, typeof(FixedInterval))]
[Union(2, typeof(BoundaryCrossing))]
[Union(3, typeof(KinematicDiscontinuity))]
public abstract record StepPolicy
{
    private StepPolicy() { }

    /// <summary>
    /// Varies step size based on curvature/velocity.
    /// </summary>
    [MessagePackObject]
    public sealed record Adaptive(
        [property: Key(0)] double MinStepTicks,
        [property: Key(1)] double MaxStepTicks,
        [property: Key(2)] double Tolerance
    ) : StepPolicy;

    /// <summary>
    /// Uniform steps in tick space.
    /// </summary>
    [MessagePackObject]
    public sealed record FixedInterval(
        [property: Key(0)] double StepTicks
    ) : StepPolicy;

    /// <summary>
    /// Steps at every plate boundary crossing.
    /// </summary>
    [MessagePackObject]
    public sealed record BoundaryCrossing : StepPolicy;

    /// <summary>
    /// Steps at every kinematics segment boundary.
    /// </summary>
    [MessagePackObject]
    public sealed record KinematicDiscontinuity : StepPolicy;

    /// <summary>
    /// Default policy (FixedInterval with 1 tick).
    /// </summary>
    public static StepPolicy Default { get; } = new FixedInterval(1.0);
}
