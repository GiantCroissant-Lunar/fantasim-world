namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Policy for comparing angles in a deterministic, stable manner.
/// </summary>
/// <remarks>
/// <para>
/// Floating-point angle comparisons can be unstable near collinear edges.
/// This policy provides two mechanisms to stabilize ordering:
/// </para>
/// <list type="bullet">
///   <item><b>Quantization</b>: Round angles to discrete bins before comparison</item>
///   <item><b>Epsilon</b>: Treat angles within epsilon as equal, falling back to tie-breakers</item>
/// </list>
/// <para>
/// The default policy uses a small epsilon (1e-12 radians ≈ 0.2 microarcseconds)
/// which is well below any meaningful geometric tolerance while still catching
/// floating-point representation differences.
/// </para>
/// </remarks>
public readonly record struct AnglePolicy
{
    /// <summary>
    /// Epsilon for angle comparison (radians). Angles within this tolerance
    /// are considered equal and fall back to tie-breakers.
    /// </summary>
    public double Epsilon { get; init; }

    /// <summary>
    /// If true, quantize angles to discrete bins before comparison.
    /// Bin size is determined by <see cref="QuantizationRadians"/>.
    /// </summary>
    public bool UseQuantization { get; init; }

    /// <summary>
    /// Quantization bin size in radians. Only used if <see cref="UseQuantization"/> is true.
    /// </summary>
    public double QuantizationRadians { get; init; }

    /// <summary>
    /// Default policy: epsilon-based comparison with 1e-12 radians tolerance.
    /// </summary>
    public static AnglePolicy Default => new()
    {
        Epsilon = 1e-12,
        UseQuantization = false,
        QuantizationRadians = 0
    };

    /// <summary>
    /// Strict policy: no tolerance, direct floating-point comparison.
    /// Use only when you need exact IEEE 754 semantics.
    /// </summary>
    public static AnglePolicy Strict => new()
    {
        Epsilon = 0,
        UseQuantization = false,
        QuantizationRadians = 0
    };

    /// <summary>
    /// Quantized policy: round angles to 1e-9 radian bins (~0.2 milliarcseconds).
    /// Useful when geometry has known precision limits.
    /// </summary>
    public static AnglePolicy Quantized(double binSizeRadians = 1e-9) => new()
    {
        Epsilon = 0,
        UseQuantization = true,
        QuantizationRadians = binSizeRadians
    };

    /// <summary>
    /// Compares two angles according to this policy.
    /// Returns 0 if angles are considered equal (within tolerance or same bin).
    /// </summary>
    /// <param name="angleA">First angle in radians.</param>
    /// <param name="angleB">Second angle in radians.</param>
    /// <returns>
    /// Negative if angleA &lt; angleB, positive if angleA &gt; angleB, zero if equal within policy.
    /// </returns>
    public int CompareAngles(double angleA, double angleB)
    {
        double a = angleA;
        double b = angleB;

        if (UseQuantization && QuantizationRadians > 0)
        {
            // Quantize to discrete bins
            a = Math.Floor(a / QuantizationRadians) * QuantizationRadians;
            b = Math.Floor(b / QuantizationRadians) * QuantizationRadians;
        }

        var diff = a - b;

        if (Epsilon > 0 && Math.Abs(diff) <= Epsilon)
        {
            return 0; // Angles are equal within tolerance
        }

        return diff.CompareTo(0.0);
    }
}
