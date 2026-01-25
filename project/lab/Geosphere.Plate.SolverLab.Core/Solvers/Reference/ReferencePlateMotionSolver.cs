using System.Collections.Immutable;
using System.Diagnostics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Solvers.Reference;

/// <summary>
/// Reference implementation of plate motion solver.
/// Prioritizes correctness and readability over performance.
/// </summary>
public sealed class ReferencePlateMotionSolver : IPlateMotionSolver
{
    public SolverMetadata Metadata => new()
    {
        Name = "Reference",
        Version = "1.0.0",
        Description = "Naive O(n²) force calculation, readable but slow",
        Complexity = "O(n²)"
    };

    public PlateMotionResult Calculate(PlateMotionInput input) => Calculate(input.Snapshot, input.TimeDeltaS);

    public PlateMotionResult Calculate(PlateMechanicsSnapshot topology, float dt)
    {
        var sw = Stopwatch.StartNew();
        var motions = new List<PlateMotion>();

        // Calculate forces on each plate
        foreach (var plate in topology.Plates)
        {
            var force = CalculateNetForce(plate, topology);
            var motion = IntegrateMotion(plate, force, dt);
            motions.Add(motion);
        }

        // Detect topology changes
        var rifts = DetectRifts(topology, motions);
        var collisions = DetectCollisions(topology, motions);

        sw.Stop();

        return new PlateMotionResult
        {
            PlateMotions = motions.ToArray(),
            NewRifts = rifts.ToArray(),
            NewCollisions = collisions.ToArray(),
            Metrics = new ComputationMetrics
            {
                ComputeTimeMs = sw.Elapsed.TotalMilliseconds,
                IterationCount = 1,
                ConvergenceError = 0
            }
        };
    }

    private Vector3d CalculateNetForce(PlateSnapshot plate, PlateMechanicsSnapshot topology)
    {
        var force = Vector3d.Zero;

        // Ridge push (from divergent boundaries)
        foreach (var boundary in topology.Boundaries.Where(b =>
            b.Type == BoundaryType.Divergent &&
            (b.PlateA == plate.PlateId || b.PlateB == plate.PlateId)))
        {
            force += CalculateRidgePush(plate, boundary);
        }

        // Slab pull (from subducting boundaries)
        foreach (var boundary in topology.Boundaries.Where(b =>
            b.Type == BoundaryType.Convergent &&
            b.SubductingPlate == plate.PlateId))
        {
            force += CalculateSlabPull(plate, boundary);
        }

        // Mantle drag (resistance)
        force += CalculateMantleDrag(plate);

        return force;
    }

    private Vector3d CalculateRidgePush(PlateSnapshot plate, BoundarySnapshot boundary)
    {
        // Placeholder logic
        return Vector3d.UnitX * 1000.0;
    }

    private Vector3d CalculateSlabPull(PlateSnapshot plate, BoundarySnapshot boundary)
    {
        // Placeholder logic
        return Vector3d.UnitY * 2000.0;
    }

    private Vector3d CalculateMantleDrag(PlateSnapshot plate)
    {
        // Placeholder logic: oppose motion (simplified as generic drag for now)
        return Vector3d.Zero; // Needs velocity state which we don't track in snapshot currently, assuming static start or implicitly handled
    }

    private PlateMotion IntegrateMotion(PlateSnapshot plate, Vector3d force, float dt)
    {
        // Simple Euler integration
        // F = ma -> a = F/m
        var acceleration = force / plate.MassKg;
        var deltaPosition = acceleration * dt * dt * 0.5;

        // Angular motion from torque
        var torque = CalculateTorque(plate, force);

        // Simplified inertia for thin shell sphere approx
        double inertia = plate.MassKg * plate.AreaM2 / 12.0;

        // Avoid divide by zero
        if (inertia < 1e-9) inertia = 1.0;

        var angularAccel = torque / inertia;
        // Approximation: torque direction is axis of rotation
        var deltaRotation = Quaterniond.FromAxisAngle(Vector3d.UnitZ, angularAccel.Length() * dt * dt * 0.5);

        return new PlateMotion
        {
            PlateId = plate.PlateId,
            DeltaPosition = deltaPosition,
            DeltaRotation = deltaRotation,
            Force = force,
            Torque = torque
        };
    }

    private Vector3d CalculateTorque(PlateSnapshot plate, Vector3d force)
    {
        // Placeholder: Torque = r x F
        // Assuming force applied at some offset from center of mass
        return Vector3d.Zero;
    }

    private List<RiftEvent> DetectRifts(PlateMechanicsSnapshot topology, List<PlateMotion> motions)
    {
        // Placeholder
        return new List<RiftEvent>();
    }

    private List<CollisionEvent> DetectCollisions(PlateMechanicsSnapshot topology, List<PlateMotion> motions)
    {
        // Placeholder
        return new List<CollisionEvent>();
    }
}
