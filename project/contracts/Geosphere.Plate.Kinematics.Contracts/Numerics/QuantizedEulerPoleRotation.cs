using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;

/// <summary>
/// Quantized Euler pole rotation payload for deterministic persistence/hashing.
///
/// Values are stored as micro-degrees (1e-6 degrees) to avoid floating ambiguity.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct QuantizedEulerPoleRotation(
    [property: Key(0)] int AxisAzimuthMicroDeg,
    [property: Key(1)] int AxisElevationMicroDeg,
    [property: Key(2)] int AngleMicroDeg)
{
    public const int MicroDegPerDeg = 1_000_000;

    public static QuantizedEulerPoleRotation Create(
        int axisAzimuthMicroDeg,
        int axisElevationMicroDeg,
        int angleMicroDeg)
    {
        axisAzimuthMicroDeg = WrapAzimuthMicroDeg(axisAzimuthMicroDeg);
        axisElevationMicroDeg = Math.Clamp(axisElevationMicroDeg, -90 * MicroDegPerDeg, 90 * MicroDegPerDeg);
        return new QuantizedEulerPoleRotation(axisAzimuthMicroDeg, axisElevationMicroDeg, angleMicroDeg);
    }

    private static int WrapAzimuthMicroDeg(int azimuth)
    {
        // Wrap to [-180, 180] degrees.
        var full = 360 * MicroDegPerDeg;
        var half = 180 * MicroDegPerDeg;

        var wrapped = azimuth % full;
        if (wrapped > half) wrapped -= full;
        if (wrapped < -half) wrapped += full;
        return wrapped;
    }

    [IgnoreMember]
    public double AxisAzimuthDeg => (double)AxisAzimuthMicroDeg / MicroDegPerDeg;

    [IgnoreMember]
    public double AxisElevationDeg => (double)AxisElevationMicroDeg / MicroDegPerDeg;

    [IgnoreMember]
    public double AngleDeg => (double)AngleMicroDeg / MicroDegPerDeg;
}
