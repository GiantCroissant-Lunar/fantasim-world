namespace FantaSim.Geosphere.Plate.Junction.Contracts.Products;

/// <summary>
/// Classification of a triple junction by boundary types (RFC-V2-0042 §6.3).
/// </summary>
/// <remarks>
/// <para>
/// Labels follow standard plate tectonics notation:
/// <list type="bullet">
///   <item><description>R = Ridge (divergent boundary)</description></item>
///   <item><description>F = Transform fault (strike-slip)</description></item>
///   <item><description>T = Trench (convergent/subduction boundary)</description></item>
/// </list>
/// </para>
/// <para>
/// Classification is derived from boundary types in alphabetical order (F &lt; R &lt; T).
/// For example, a junction with Ridge-Trench-Trench boundaries → RTT.
/// </para>
/// </remarks>
public enum JunctionClassification
{
    /// <summary>Transform-Transform-Transform: three strike-slip boundaries.</summary>
    FFF,

    /// <summary>Transform-Transform-Trench: two transforms, one convergent.</summary>
    FFT,

    /// <summary>Transform-Trench-Trench: one transform, two convergent.</summary>
    FTT,

    /// <summary>Ridge-Transform-Transform: one divergent, two transforms.</summary>
    RFF,

    /// <summary>Ridge-Transform-Trench: one each of ridge, transform, trench.</summary>
    RFT,

    /// <summary>Ridge-Ridge-Transform: two divergent, one transform.</summary>
    RRF,

    /// <summary>Ridge-Ridge-Ridge: three divergent boundaries (spreading center intersection).</summary>
    RRR,

    /// <summary>Ridge-Ridge-Trench: two divergent, one convergent.</summary>
    RRT,

    /// <summary>Ridge-Trench-Trench: one divergent, two convergent.</summary>
    RTT,

    /// <summary>Trench-Trench-Trench: three convergent boundaries.</summary>
    TTT,

    /// <summary>Unknown or unclassifiable junction (missing types, non-triple, etc.).</summary>
    Unknown
}
