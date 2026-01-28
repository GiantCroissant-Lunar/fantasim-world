using System.Runtime.InteropServices;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Velocity.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// A single sample along a motion path (RFC-V2-0035 §7.1).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MotionPathSample(
    [property: Key(0)] CanonicalTick Tick,
    [property: Key(1)] Point3 Position,
    [property: Key(2)] Velocity3d Velocity,
    [property: Key(3)] int StepIndex
);
