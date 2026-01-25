namespace FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

/// <summary>
/// Solver interface for plate tectonics simulation.
/// Implementations must be stateless and deterministic.
/// </summary>
public interface IPlateMotionSolver : ISolver<PlateMotionInput, PlateMotionResult>
{
    /// <summary>
    /// Calculate plate motions for a single time step.
    /// </summary>
    /// <param name="topology">Current plate mechanics snapshot (immutable).</param>
    /// <param name="dt">Time delta in seconds.</param>
    /// <returns>Motion result containing deltas and events.</returns>
    PlateMotionResult Calculate(PlateMechanicsSnapshot topology, float dt);

    // Explicit interface implementation to satisfy ISolver
    PlateMotionResult ISolver<PlateMotionInput, PlateMotionResult>.Calculate(PlateMotionInput input)
    {
        return Calculate(input.Snapshot, input.TimeDeltaS);
    }
}
