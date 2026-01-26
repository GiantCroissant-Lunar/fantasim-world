using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;

/// <summary>
/// Quantized Euler pole rotation payload for deterministic persistence/hashing.
///
/// Values are stored as micro-degrees (1e-6 degrees) to avoid floating ambiguity.
/// </summary>
[MessagePackObject]
public readonly record struct QuantizedEulerPoleRotation(
    [property: Key(0)] int PoleLonMicroDeg,
    [property: Key(1)] int PoleLatMicroDeg,
    [property: Key(2)] int AngleMicroDeg)
{
    public const int MicroDegPerDeg = 1_000_000;

    public static QuantizedEulerPoleRotation Create(
        int poleLonMicroDeg,
        int poleLatMicroDeg,
        int angleMicroDeg)
    {
        poleLonMicroDeg = WrapLonMicroDeg(poleLonMicroDeg);
        poleLatMicroDeg = Math.Clamp(poleLatMicroDeg, -90 * MicroDegPerDeg, 90 * MicroDegPerDeg);
        return new QuantizedEulerPoleRotation(poleLonMicroDeg, poleLatMicroDeg, angleMicroDeg);
    }

    private static int WrapLonMicroDeg(int lon)
    {
        // Wrap to [-180, 180] degrees.
        var full = 360 * MicroDegPerDeg;
        var half = 180 * MicroDegPerDeg;

        var wrapped = lon % full;
        if (wrapped > half) wrapped -= full;
        if (wrapped < -half) wrapped += full;
        return wrapped;
    }

    [IgnoreMember]
    public double PoleLonDeg => (double)PoleLonMicroDeg / MicroDegPerDeg;

    [IgnoreMember]
    public double PoleLatDeg => (double)PoleLatMicroDeg / MicroDegPerDeg;

    [IgnoreMember]
    public double AngleDeg => (double)AngleMicroDeg / MicroDegPerDeg;
}
