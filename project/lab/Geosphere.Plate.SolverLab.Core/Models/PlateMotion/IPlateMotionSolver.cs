using FantaSim.Geosphere.Plate.SolverLab.Core.Abstractions;
using FantaSim.Geosphere.Plate.SolverLab.Core.Models.PlateMotion;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Models.PlateMotion;

/// <summary>
/// Solver interface for plate tectonics simulation.
/// Implementations must be stateless and deterministic.
/// </summary>
public interface IPlateMotionSolver : ISolver<PlateMotionInput, PlateMotionResult>
{
    /// <summary>
    /// Calculate plate motions for a single time step.
    /// </summary>
    /// <param name="topology">Current plate topology snapshot (immutable).</param>
    /// <param name="dt">Time delta in seconds.</param>
    /// <returns>Motion result containing deltas and events.</returns>
    PlateMotionResult Calculate(PlateTopologySnapshot topology, float dt);

    // Explicit interface implementation to satisfy ISolver
    PlateMotionResult ISolver<PlateMotionInput, PlateMotionResult>.Calculate(PlateMotionInput input)
    {
        return Calculate(input.Snapshot, input.TimeDeltaS);
    }
}
