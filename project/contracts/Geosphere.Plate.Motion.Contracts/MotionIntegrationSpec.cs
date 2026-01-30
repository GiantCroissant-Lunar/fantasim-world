using System.Runtime.InteropServices;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Specification for motion path integration (RFC-V2-0035 §6).
/// </summary>
/// <remarks>
/// <para>
/// <b>Default values:</b> StepTicks = 1, MaxSteps = 1000, Method = Euler.
/// </para>
/// <para>
/// <b>Note:</b> Use <see cref="Default"/> for default specification, or specify all parameters explicitly.
/// The parameterless constructor uses struct default values (0), not these documented defaults.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct MotionIntegrationSpec
{
    /// <summary>Default integration specification with StepTicks=1, MaxSteps=1000, Method=Euler.</summary>
    public static MotionIntegrationSpec Default => new(1, 1000, IntegrationMethod.Euler);

    /// <summary>Time step size in canonical ticks. Default: 1.</summary>
    public int StepTicks { get; init; }

    /// <summary>Maximum number of integration steps. Default: 1000.</summary>
    public int MaxSteps { get; init; }

    /// <summary>Integration method. Default: Euler.</summary>
    public IntegrationMethod Method { get; init; }

    /// <summary>
    /// Creates a motion integration specification.
    /// </summary>
    /// <param name="stepTicks">Time step size in canonical ticks. Must be positive.</param>
    /// <param name="maxSteps">Maximum number of integration steps. Must be positive.</param>
    /// <param name="method">Integration method.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when stepTicks or maxSteps is not positive.</exception>
    public MotionIntegrationSpec(int stepTicks = 1, int maxSteps = 1000, IntegrationMethod method = IntegrationMethod.Euler)
    {
        if (stepTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepTicks), stepTicks, "StepTicks must be positive.");
        if (maxSteps <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSteps), maxSteps, "MaxSteps must be positive.");

        StepTicks = stepTicks;
        MaxSteps = maxSteps;
        Method = method;
    }
}
