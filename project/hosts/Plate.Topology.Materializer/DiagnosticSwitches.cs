using System.Diagnostics;

namespace Plate.Topology.Materializer;

/// <summary>
/// Diagnostic trace switches for Plate.Topology.Materializer.
///
/// Enable these switches via app.config, environment variables, or code:
/// <code>
/// // Via code (useful for tests)
/// DiagnosticSwitches.MaterializationOptimization.Level = TraceLevel.Info;
///
/// // Via environment variable
/// PLATE_TOPOLOGY_MATERIALIZATION_OPTIMIZATION=4  // TraceLevel.Info
/// </code>
///
/// Trace levels:
/// - Off (0): No tracing
/// - Error (1): Only errors
/// - Warning (2): Errors and warnings
/// - Info (3): Informational messages (optimization decisions, etc.)
/// - Verbose (4): Detailed diagnostic output
/// </summary>
public static class DiagnosticSwitches
{
    /// <summary>
    /// Controls tracing for materialization optimization decisions.
    ///
    /// When enabled at Info level, logs when Auto mode resolves to
    /// BreakOnFirstBeyondTick vs ScanAll, helping diagnose performance differences.
    /// </summary>
    public static readonly TraceSwitch MaterializationOptimization = new(
        "Plate.Topology.MaterializationOptimization",
        "Controls tracing for tick materialization optimization decisions");

    /// <summary>
    /// Controls tracing for capability validation warnings.
    ///
    /// When enabled, logs warnings when capability metadata appears inconsistent
    /// with runtime behavior (e.g., monotone flag set but Reject policy not used).
    /// </summary>
    public static readonly TraceSwitch CapabilityValidation = new(
        "Plate.Topology.CapabilityValidation",
        "Controls tracing for stream capability validation warnings");
}
