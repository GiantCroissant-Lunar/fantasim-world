using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

/// <summary>
/// Specifies the method for computing area weights in net rotation calculation.
/// </summary>
public enum AreaWeightingMethod
{
    /// <summary>
    /// Use actual plate areas derived from topology. Default and recommended.
    /// </summary>
    TopologyDerived,

    /// <summary>
    /// Use uniform weights (all plates weighted equally). Fallback when areas unavailable.
    /// </summary>
    Uniform
}

/// <summary>
/// Computes the mantle frame transform using area-weighted net rotation.
/// RFC-V2-0046 Section 3.1: The mantle frame achieves zero net lithospheric rotation
/// via area-weighted computation of all plate rotations.
/// </summary>
public static class MantleFrameCalculator
{
    /// <summary>
    /// Computes the net rotation of all plates weighted by their areas.
    /// </summary>
    /// <param name="plateRotations">Dictionary mapping plate IDs to their finite rotations.</param>
    /// <param name="plateAreas">Dictionary mapping plate IDs to their areas (in any consistent unit).</param>
    /// <param name="weightingMethod">The weighting method to use.</param>
    /// <returns>The area-weighted net rotation of all plates.</returns>
    public static FiniteRotation ComputeNetRotation(
        IReadOnlyDictionary<PlateId, FiniteRotation> plateRotations,
        IReadOnlyDictionary<PlateId, double> plateAreas,
        AreaWeightingMethod weightingMethod)
    {
        if (plateRotations.Count == 0)
        {
            return FiniteRotation.Identity;
        }

        return weightingMethod switch
        {
            AreaWeightingMethod.TopologyDerived => ComputeAreaWeightedNetRotation(plateRotations, plateAreas),
            AreaWeightingMethod.Uniform => ComputeUniformWeightedNetRotation(plateRotations),
            _ => throw new ArgumentOutOfRangeException(nameof(weightingMethod))
        };
    }

    /// <summary>
    /// Computes the mantle frame transform as the inverse of the net rotation.
    /// Per RFC-V2-0046: mantle frame applies the inverse of net rotation to achieve
    /// zero net lithospheric rotation.
    /// </summary>
    /// <param name="plateRotations">Dictionary mapping plate IDs to their finite rotations.</param>
    /// <param name="plateAreas">Dictionary mapping plate IDs to their areas.</param>
    /// <param name="weightingMethod">The weighting method to use.</param>
    /// <returns>The mantle frame transform (inverse of net rotation).</returns>
    public static FiniteRotation GetMantleFrameTransform(
        IReadOnlyDictionary<PlateId, FiniteRotation> plateRotations,
        IReadOnlyDictionary<PlateId, double> plateAreas,
        AreaWeightingMethod weightingMethod = AreaWeightingMethod.TopologyDerived)
    {
        var netRotation = ComputeNetRotation(plateRotations, plateAreas, weightingMethod);
        return netRotation.Inverted();
    }

    /// <summary>
    /// Computes area-weighted average rotation using quaternion averaging.
    /// Uses weighted quaternion averaging which is valid for small rotation differences.
    /// </summary>
    private static FiniteRotation ComputeAreaWeightedNetRotation(
        IReadOnlyDictionary<PlateId, FiniteRotation> plateRotations,
        IReadOnlyDictionary<PlateId, double> plateAreas)
    {
        // Accumulate weighted quaternion components
        double sumX = 0, sumY = 0, sumZ = 0, sumW = 0;
        double totalWeight = 0;

        // Use the first quaternion as reference for hemisphere consistency
        Quaterniond? referenceQ = null;

        foreach (var (plateId, rotation) in plateRotations)
        {
            // Get area weight, defaulting to 0 if plate area unknown
            if (!plateAreas.TryGetValue(plateId, out var area) || area <= 0)
            {
                continue;
            }

            var q = rotation.Orientation;

            // Ensure all quaternions are in the same hemisphere (q and -q represent the same rotation)
            if (referenceQ is null)
            {
                referenceQ = q;
            }
            else
            {
                var dot = (referenceQ.Value.X * q.X) + (referenceQ.Value.Y * q.Y) +
                          (referenceQ.Value.Z * q.Z) + (referenceQ.Value.W * q.W);
                if (dot < 0)
                {
                    q = new Quaterniond(-q.X, -q.Y, -q.Z, -q.W);
                }
            }

            sumX += q.X * area;
            sumY += q.Y * area;
            sumZ += q.Z * area;
            sumW += q.W * area;
            totalWeight += area;
        }

        if (totalWeight <= double.Epsilon)
        {
            return FiniteRotation.Identity;
        }

        // Normalize to get weighted average quaternion
        var avgX = sumX / totalWeight;
        var avgY = sumY / totalWeight;
        var avgZ = sumZ / totalWeight;
        var avgW = sumW / totalWeight;

        // Renormalize the quaternion (required after averaging)
        var norm = Math.Sqrt((avgX * avgX) + (avgY * avgY) + (avgZ * avgZ) + (avgW * avgW));
        if (norm <= double.Epsilon)
        {
            return FiniteRotation.Identity;
        }

        var normalizedQ = new Quaterniond(avgX / norm, avgY / norm, avgZ / norm, avgW / norm);
        return new FiniteRotation(normalizedQ);
    }

    /// <summary>
    /// Computes uniform-weighted average rotation (all plates weighted equally).
    /// </summary>
    private static FiniteRotation ComputeUniformWeightedNetRotation(
        IReadOnlyDictionary<PlateId, FiniteRotation> plateRotations)
    {
        if (plateRotations.Count == 0)
        {
            return FiniteRotation.Identity;
        }

        // Accumulate quaternion components
        double sumX = 0, sumY = 0, sumZ = 0, sumW = 0;
        Quaterniond? referenceQ = null;

        foreach (var rotation in plateRotations.Values)
        {
            var q = rotation.Orientation;

            // Ensure all quaternions are in the same hemisphere
            if (referenceQ is null)
            {
                referenceQ = q;
            }
            else
            {
                var dot = (referenceQ.Value.X * q.X) + (referenceQ.Value.Y * q.Y) +
                          (referenceQ.Value.Z * q.Z) + (referenceQ.Value.W * q.W);
                if (dot < 0)
                {
                    q = new Quaterniond(-q.X, -q.Y, -q.Z, -q.W);
                }
            }

            sumX += q.X;
            sumY += q.Y;
            sumZ += q.Z;
            sumW += q.W;
        }

        // Normalize
        var norm = Math.Sqrt((sumX * sumX) + (sumY * sumY) + (sumZ * sumZ) + (sumW * sumW));
        if (norm <= double.Epsilon)
        {
            return FiniteRotation.Identity;
        }

        var normalizedQ = new Quaterniond(sumX / norm, sumY / norm, sumZ / norm, sumW / norm);
        return new FiniteRotation(normalizedQ);
    }
}
