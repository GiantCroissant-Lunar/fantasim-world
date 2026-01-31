namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Configuration for motion path integration steps (RFC-V2-0049 §3.3).
/// </summary>
public abstract record StepPolicy
{
    private StepPolicy() { }

    /// <summary>
    /// Varies step size based on curvature/velocity.
    /// </summary>
    public sealed record Adaptive(double MinStepTicks, double MaxStepTicks, double Tolerance) : StepPolicy;

    /// <summary>
    /// Uniform steps in tick space.
    /// </summary>
    public sealed record FixedInterval(double StepTicks) : StepPolicy;

    /// <summary>
    /// Steps at every plate boundary crossing.
    /// </summary>
    public sealed record BoundaryCrossing : StepPolicy;

    /// <summary>
    /// Steps at every kinematics segment boundary.
    /// </summary>
    public sealed record KinematicDiscontinuity : StepPolicy;

    /// <summary>
    /// Default policy (FixedInterval with 1 tick).
    /// </summary>
    public static StepPolicy Default { get; } = new FixedInterval(1.0);
}
