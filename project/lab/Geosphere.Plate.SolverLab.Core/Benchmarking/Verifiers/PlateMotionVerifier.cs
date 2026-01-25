using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.SolverLab.Core.Models.PlateMotion;
using FantaSim.Geosphere.Plate.SolverLab.Core.Numerics;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking.Verifiers;

public sealed class PlateMotionVerifier : IVerifier<PlateMotionResult>
{
    private readonly double _positionToleranceM = 1.0;        // 1 meter
    private readonly double _rotationToleranceRad = 1e-6;     // ~0.00006 degrees

    public bool Verify(PlateMotionResult expected, PlateMotionResult actual, out string? error)
    {
        var errors = new List<string>();

        // Check all plate motions
        foreach (var expectedMotion in expected.PlateMotions)
        {
            var actualMotion = actual.PlateMotions
                .FirstOrDefault(m => m.PlateId == expectedMotion.PlateId);

            if (actualMotion.PlateId == default)
            {
                errors.Add($"Missing motion for plate {expectedMotion.PlateId}");
                continue;
            }

            // Position check
            var positionDiff = (expectedMotion.DeltaPosition - actualMotion.DeltaPosition).Length();
            if (positionDiff > _positionToleranceM)
            {
                errors.Add($"Plate {expectedMotion.PlateId}: position diff {positionDiff:F3}m exceeds tolerance");
            }

            // Rotation check
            var rotationDiff = Quaterniond.Angle(expectedMotion.DeltaRotation, actualMotion.DeltaRotation);
            if (rotationDiff > _rotationToleranceRad)
            {
                errors.Add($"Plate {expectedMotion.PlateId}: rotation diff {rotationDiff:E3}rad exceeds tolerance");
            }
        }

        // Check topology events
        if (expected.NewRifts.Length != actual.NewRifts.Length)
        {
            errors.Add($"Rift count mismatch: expected {expected.NewRifts.Length}, got {actual.NewRifts.Length}");
        }

        if (errors.Count > 0)
        {
            error = string.Join("; ", errors);
            return false;
        }

        error = null;
        return true;
    }
}
