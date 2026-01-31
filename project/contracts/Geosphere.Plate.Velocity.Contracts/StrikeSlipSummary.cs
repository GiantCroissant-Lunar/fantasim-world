using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Strike-slip motion summary for transform boundaries (RFC-V2-0048 §5.4).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct StrikeSlipSummary(
    [property: Key(0)] double MaxStrikeSlipRate,
    [property: Key(1)] double MeanStrikeSlipRate,
    [property: Key(2)] double TotalStrikeSlipLength,
    [property: Key(3)] int StrikeSlipSampleCount,
    [property: Key(4)] int RightLateralCount,
    [property: Key(5)] int LeftLateralCount
);
