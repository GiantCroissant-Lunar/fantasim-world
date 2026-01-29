using FantaSim.Geosphere.Plate.Junction.Contracts.Diagnostics;
using FantaSim.Geosphere.Plate.Junction.Contracts.Products;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Solvers;

/// <summary>
/// Solver for building junction products and diagnostics (RFC-V2-0042 §9.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Determinism:</b> All methods MUST produce identical results for identical inputs.
/// </para>
/// <para>
/// <b>Pure computation:</b> Solvers MUST NOT perform I/O or mutate truth.
/// </para>
/// </remarks>
public interface IJunctionAnalyzer
{
    /// <summary>
    /// Builds the junction set at the given tick.
    /// </summary>
    /// <param name="tick">Query tick.</param>
    /// <param name="topology">Materialized topology state at tick.</param>
    /// <param name="options">Analysis options (uses defaults if null).</param>
    /// <returns>Complete junction set with incidents and classifications.</returns>
    /// <remarks>
    /// <para>
    /// Incident ordering follows RFC-V2-0042 §10.1: sorted by angle (CCW from +X),
    /// ties broken by BoundaryId.
    /// </para>
    /// <para>
    /// Classification requires boundary types to be available in topology.
    /// If types are missing, junctions are classified as <see cref="JunctionClassification.Unknown"/>.
    /// </para>
    /// </remarks>
    JunctionSet BuildJunctionSet(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        JunctionAnalysisOptions? options = null);

    /// <summary>
    /// Computes kinematic diagnostics for all junctions.
    /// Requires velocity solver from RFC-V2-0033.
    /// </summary>
    /// <param name="tick">Query tick.</param>
    /// <param name="junctions">Junction set to analyze.</param>
    /// <param name="topology">Materialized topology state at tick.</param>
    /// <param name="kinematics">Materialized kinematics state at tick.</param>
    /// <param name="velocitySolver">Velocity computation solver.</param>
    /// <param name="options">Analysis options (uses defaults if null).</param>
    /// <returns>Complete diagnostics including closure analysis.</returns>
    JunctionDiagnostics Diagnose(
        CanonicalTick tick,
        JunctionSet junctions,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        IPlateVelocitySolver velocitySolver,
        JunctionAnalysisOptions? options = null);

    /// <summary>
    /// Computes closure diagnostic for a single junction.
    /// </summary>
    /// <param name="junction">The junction to diagnose.</param>
    /// <param name="tick">Query tick.</param>
    /// <param name="topology">Materialized topology state at tick.</param>
    /// <param name="kinematics">Materialized kinematics state at tick.</param>
    /// <param name="velocitySolver">Velocity computation solver.</param>
    /// <param name="options">Analysis options (uses defaults if null).</param>
    /// <returns>Closure diagnostic for the junction.</returns>
    JunctionClosureDiagnostic DiagnoseJunction(
        JunctionInfo junction,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        IPlateVelocitySolver velocitySolver,
        JunctionAnalysisOptions? options = null);
}
