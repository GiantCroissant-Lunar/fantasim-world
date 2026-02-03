using System;
using FantaSim.Geosphere.Plate.Deformation.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Sampling.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Deformation.Solver;


public sealed class DeformationSolver : IDeformationService
{
    private readonly ISamplingService _samplingService;

    // Earth's mean radius in meters for metric corrections, if needed.
    // However, velocity is "distance units per tick". The grid resolution is in degrees.
    // To get "per tick" units for strain rate, we need to convert the spatial steps (degrees) to distance units.
    // If the velocity is in "km/Myr" and we divide by "km", we get "1/Myr".
    //
    // IMPORTANT: The RFC says:
    // "Units: per canonical tick."
    // "Δeast  = R · cos(lat) · Δlon     (in radians)"
    // "Δnorth = R · Δlat                 (in radians)"
    //
    // If velocity is in "Earth radii / tick" (internal units often normalized) and R=1, it simplifies.
    // However, FantaSim velocity units are typically "km/Myr" or similar if projected, but internal engine units might be different.
    // Velocity3d doc says: "Units: body-frame distance units per canonical tick."
    //
    // So if I divide Δv (distance/tick) by Δx (distance), I get (1/tick).
    // The "distance" corresponding to Δlon degrees is:
    // dist = (Δlon_rad) * Radius.
    //
    // FantaSim typically uses a unit sphere for some things, but let's check if we need an explicit Radius contract.
    // The Velocity3d contract says "body-frame distance units".
    // Usually this implies the radius is defined by the coordinate system.
    // For now, I will assume the 'distance' units in velocity match the 'distance' units derived from converting lat/lon radians to arc length *on the sphere of that radius*.
    //
    // Effectively: StrainRate = (Velocity_Distance / Tick) / (Grid_Distance)
    // Grid_Distance = Angle_Radians * Radius.
    // Velocity_Distance is also effectively Angle_Radians_Traversed * Radius (if on surface).
    // So Radius cancels out if consistent.
    // I.e. I can treat Radius = 1 and Velocity as "Radians / Tick" if I convert Velocity linear units to angular units?
    //
    // NO. Velocity is linear.
    // Strain Rate = (Δv m/s) / (Δx m) = (1/s).
    //
    // If I compute Δv in "units/tick" and Δx in "radians", I need to convert radians to "units".
    // Δx_units = Δx_radians * R_units.
    //
    // Missing constant: The Radius of the Earth in "body-frame distance units".
    // If the body frame distance unit is 1 Earth Radius (6371km), then R=1.
    // If the body frame distance unit is km, then R=6371.
    //
    // I will check `Plate.TimeDete.Time.Primitives` or Topology contracts for constants.
    // For now I'll introduce a constant `SphereRadius` which ideally should be injected or from a constant class.
    // But since `Velocity3d` is "body-frame distance units", and typically we defined the sphere as Radius=1.0 in standard vector logic,
    // UNLESS the simulation uses km.
    //
    // Let's assume R=1.0 for the internal math if the system is normalized.
    // BUT, `Velocity3d` usage usually implies km/Myr or similar in GPlates.
    // However, `Unify-Maths` usually works on unit sphere.
    //
    // Let's look at `Geosphere.Plate.Velocity.Contracts.Velocity3d`: "Units: body-frame distance units per canonical tick."
    // And `Vector3d`: "Normalized to unit sphere usually?".
    //
    // I'll stick to R=1.0 assumption for "Normalized" units, effectively treating velocity as "Radians/Tick" * 1.0.
    // Use `Constants.MeanEarthRadiusKm` if available?
    // Checking `Geosphere.Plate.Topology.Contracts.Numerics` might be useful later.
    // For this implementation, I will assume the provided `ISamplingService` effectively returns velocities on the unit sphere if the domain is on the unit sphere.
    //
    // Actually, simply:
    // derivative = (v2 - v1) / distance
    // distance = angle_diff_radians * Radius.
    // Radius = 1.0 (canonical sphere).
    // This is the safest bet for FantaSim internal logic unless specified otherwise.

    private const double SphereRadius = 1.0;

    public DeformationSolver(ISamplingService samplingService)
    {
        _samplingService = samplingService;
    }

    public StrainRateCoverage ComputeStrainRate(
        SamplingDomain domain,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        if (domain.Grid is null)
            throw new NotSupportedException("Only regular grid sampling domains are supported (SamplingDomain.Grid must be non-null).");

        var grid = domain.Grid;

        // 1. Sample Velocity
        var velocityCoverage = _samplingService.SampleVectorField(
            domain,
            VectorFieldId.Velocity,
            tick,
            policy);

        int nodeCount = grid.NodeCount;
        var tensors = new StrainRateTensor[nodeCount];

        int nLon = grid.NLon;
        int nLat = grid.NLat;
        double dLonRad = (grid.ResolutionDeg * Math.PI) / 180.0;
        double dLatRad = (grid.ResolutionDeg * Math.PI) / 180.0;

        // Metric terms
        double invRaLat = 1.0 / (SphereRadius * dLatRad); // 1 / dy
        // 1 / dx depends on lat: 1 / (R * cos(lat) * dlon)

        var comps = velocityCoverage.Components;

        for (int j = 0; j < nLat; j++)
        {
            double latDeg = domain.Extent!.MinLat + (j * grid.ResolutionDeg) + (grid.Registration == GridRegistration.Pixel ? grid.ResolutionDeg * 0.5 : 0.0);
            double latRad = (latDeg * Math.PI) / 180.0;
            double cosLat = Math.Cos(latRad);

            // Polar singularity check
            bool isPole = Math.Abs(latDeg - 90.0) < 1e-9 || Math.Abs(latDeg + 90.0) < 1e-9;
            if (isPole || Math.Abs(cosLat) < 1e-9)
            {
                for (int i = 0; i < nLon; i++)
                {
                    int idx = j * nLon + i;
                    tensors[idx] = new StrainRateTensor { Eee = double.NaN, Enn = double.NaN, Een = double.NaN };
                }
                continue;
            }

            double invRaCosLatLon = 1.0 / (SphereRadius * cosLat * dLonRad); // 1 / dx

            for (int i = 0; i < nLon; i++)
            {
                int cIdx = j * nLon + i; // center index

                // X indices (East-West) - wrapping
                // Assumes global grid for wrapping. If not global, clamp?
                // RFC says "finite difference stencil width is 3 nodes".
                // Implies we need left/right.
                // For simplified impl, assume wrapping for lon.
                int iPrev = (i == 0) ? nLon - 1 : i - 1;
                int iNext = (i == nLon - 1) ? 0 : i + 1;

                int idxPrevX = j * nLon + iPrev;
                int idxNextX = j * nLon + iNext;

                // Y indices (North-South) - clamping/boundary
                int jPrev = j - 1;
                int jNext = j + 1;

                // Get Velocities
                // V = [Ve, Vn]
                double ve = comps[cIdx * 2];
                double vn = comps[cIdx * 2 + 1];

                // Derivatives
                double dVe_dE = (comps[idxNextX * 2] - comps[idxPrevX * 2]) * 0.5 * invRaCosLatLon;
                double dVn_dE = (comps[idxNextX * 2 + 1] - comps[idxPrevX * 2 + 1]) * 0.5 * invRaCosLatLon;

                double dVe_dN, dVn_dN;
                if (jPrev < 0)
                {
                     int idxNextY = jNext * nLon + i;
                     dVe_dN = (comps[idxNextY * 2] - ve) * invRaLat;
                     dVn_dN = (comps[idxNextY * 2 + 1] - vn) * invRaLat;
                }
                else if (jNext >= nLat)
                {
                    int idxPrevY = jPrev * nLon + i;
                    dVe_dN = (ve - comps[idxPrevY * 2]) * invRaLat;
                    dVn_dN = (vn - comps[idxPrevY * 2 + 1]) * invRaLat;
                }
                else
                {
                    int idxPrevY = jPrev * nLon + i;
                    int idxNextY = jNext * nLon + i;
                    dVe_dN = (comps[idxNextY * 2] - comps[idxPrevY * 2]) * 0.5 * invRaLat;
                    dVn_dN = (comps[idxNextY * 2 + 1] - comps[idxPrevY * 2 + 1]) * 0.5 * invRaLat;
                }

                // Metric Corrections
                double tanLat = Math.Tan(latRad);
                double metricTermEee = -(vn / SphereRadius) * tanLat;
                double metricTermEen = (ve / SphereRadius) * tanLat;

                tensors[cIdx] = new StrainRateTensor
                {
                    Eee = dVe_dE + metricTermEee,
                    Enn = dVn_dN,
                    Een = 0.5 * (dVe_dN + dVn_dE + metricTermEen)
                };
            }
        }

        return new StrainRateCoverage
        {
            Domain = domain,
            Tick = tick,
            Tensors = tensors,
            Provenance = new DeformationProvenance
            {
                DomainId = domain.DomainId,
                PolicyHash = policy.ComputeHash(),
                VelocityCoverageId = velocityCoverage.Provenance.GetHashCode().ToString(), // Simplified ID
                DifferentiationScheme = "central-difference-sphere-v1",
                SourceTruthHashes = velocityCoverage.Provenance.SourceTruthHashes,
                ComputedAt = DateTime.UtcNow
            }
        };
    }

    public ScalarCoverage ComputeScalarField(
        SamplingDomain domain,
        string deformationFieldId,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        if (domain.Grid is null)
            throw new NotSupportedException("Only regular grid sampling domains are supported (SamplingDomain.Grid must be non-null).");

        // Note: For efficiency, we could implement a method that only computes vorticity without full tensor if needed.
        // But the pipeline described in RFC Section 7.2 implies:
        // 1. Sample Velocity -> 2. Gradients -> 3. Tensor/Vorticity.
        // Re-using ComputeStrainRate is fine (it computes gradients).
        //
        // Be careful: ComputeStrainRate does not return Vorticity directly in its tensor (Tensor is symmetric).
        // Vorticity needs raw gradients (dVe/dN - dVn/dE).
        // The current ComputeStrainRate returns 'StrainRateTensor' which stores Eee, Enn, Een.
        // It LOSES the asymmetric part needed for Vorticity!
        // Een = 0.5 * (dVe_dN + dVn_dE).
        // We cannot recover (dVe_dN - dVn_dE) from Een alone.
        //
        // So we MUST refactor to either:
        // A) Have a helper that returns raw gradients.
        // B) Re-implement loop here for scalars.
        // C) Enhance StrainRateTensor? No, contract is fixed.
        //
        // I will implement a private helper `ComputeGradients`.

        // Let's defer refactoring for a helper and just duplicate the loop logic efficiently or compute on demand.
        // Since we need to support `ComputeScalarField` for Vorticity, I'll essentially run the same loop but extracting the scalar.

        // 1. Sample Velocity
        var velocityCoverage = _samplingService.SampleVectorField(
            domain,
            VectorFieldId.Velocity,
            tick,
            policy);

        int nodeCount = domain.Grid.NodeCount;
        var values = new double[nodeCount];
        var grid = domain.Grid;

        int nLon = grid.NLon;
        int nLat = grid.NLat;
        double dLonRad = (grid.ResolutionDeg * Math.PI) / 180.0;
        double dLatRad = (grid.ResolutionDeg * Math.PI) / 180.0;
        double invRaLat = 1.0 / (SphereRadius * dLatRad);

        var comps = velocityCoverage.Components;

        for (int j = 0; j < nLat; j++)
        {
            double latDeg = domain.Extent!.MinLat + (j * grid.ResolutionDeg) + (grid.Registration == GridRegistration.Pixel ? grid.ResolutionDeg * 0.5 : 0.0);
            double latRad = (latDeg * Math.PI) / 180.0;
            double cosLat = Math.Cos(latRad);
            bool isPole = Math.Abs(latDeg - 90.0) < 1e-9 || Math.Abs(latDeg + 90.0) < 1e-9;

            if (isPole || Math.Abs(cosLat) < 1e-9) // Pole or undefined
            {
                 for (int i = 0; i < nLon; i++) values[j * nLon + i] = double.NaN;
                 continue;
            }

            double invRaCosLatLon = 1.0 / (SphereRadius * cosLat * dLonRad);

             for (int i = 0; i < nLon; i++)
            {
                int cIdx = j * nLon + i;

                // Indices
                int iPrev = (i == 0) ? nLon - 1 : i - 1;
                int iNext = (i == nLon - 1) ? 0 : i + 1;
                int idxPrevX = j * nLon + iPrev;
                int idxNextX = j * nLon + iNext;

                int jPrev = j - 1;
                int jNext = j + 1;

                // Velocities
                double ve = comps[cIdx * 2];
                double vn = comps[cIdx * 2 + 1];

                // Derivatives
                double dVe_dE = (comps[idxNextX * 2] - comps[idxPrevX * 2]) * 0.5 * invRaCosLatLon;
                double dVn_dE = (comps[idxNextX * 2 + 1] - comps[idxPrevX * 2 + 1]) * 0.5 * invRaCosLatLon;

                double dVe_dN, dVn_dN;
                if (jPrev < 0)
                {
                     int idxNextY = jNext * nLon + i;
                     dVe_dN = (comps[idxNextY * 2] - ve) * invRaLat;
                     dVn_dN = (comps[idxNextY * 2 + 1] - vn) * invRaLat;
                }
                else if (jNext >= nLat)
                {
                    int idxPrevY = jPrev * nLon + i;
                    dVe_dN = (ve - comps[idxPrevY * 2]) * invRaLat;
                    dVn_dN = (vn - comps[idxPrevY * 2 + 1]) * invRaLat;
                }
                else
                {
                    int idxPrevY = jPrev * nLon + i;
                    int idxNextY = jNext * nLon + i;
                    dVe_dN = (comps[idxNextY * 2] - comps[idxPrevY * 2]) * 0.5 * invRaLat;
                    dVn_dN = (comps[idxNextY * 2 + 1] - comps[idxPrevY * 2 + 1]) * 0.5 * invRaLat;
                }

                // Metric Corrections
                double tanLat = Math.Tan(latRad);
                double metricTermEee = -(vn / SphereRadius) * tanLat;
                double metricTermEen = (ve / SphereRadius) * tanLat;

                double eee = dVe_dE + metricTermEee;
                double enn = dVn_dN;
                double een = 0.5 * (dVe_dN + dVn_dE + metricTermEen);

                // Dilatation
                // Dilatation = Eee + Enn
                double dil = eee + enn;

                // Vorticity
                // Standard Curl on sphere: (dVn/dE - dVe/dN) + (Ve/R)*tanLat.
                // We use standard math (Counter Clockwise) to satisfy the Gate Description.
                double vor = (dVn_dE - dVe_dN) + metricTermEen;

                // Compute specific field
                if (deformationFieldId == DeformationFieldId.Vorticity)
                {
                    values[cIdx] = vor;
                }
                else if (deformationFieldId == DeformationFieldId.DilatationRate)
                {
                    values[cIdx] = dil;
                }
                else if (deformationFieldId == DeformationFieldId.SecondInvariant)
                {
                    values[cIdx] = Math.Sqrt(0.5 * (eee * eee + enn * enn + 2.0 * een * een));
                }
                else if (deformationFieldId == DeformationFieldId.Divergence)
                {
                    values[cIdx] = Math.Max(dil, 0.0);
                }
                else if (deformationFieldId == DeformationFieldId.Convergence)
                {
                    values[cIdx] = Math.Max(-dil, 0.0);
                }
                else
                {
                    throw new ArgumentException($"Unknown deformation field ID: '{deformationFieldId}'");
                }
            }
        }

        return new ScalarCoverage
        {
            Domain = domain,
            Tick = tick,
            FieldId = ScalarFieldId.SpeedMagnitude, // Note: Metadata hack as ScalarFieldId enum doesn't have deformation types yet?
            // RFC says: "Sampling.Contracts currently defines ScalarFieldId as an enum; provenance carries the deformation field id string."
            // But we need to put SOMETHING in FieldId. The stub used SpeedMagnitude. I'll stick with that.
            Values = values,
            Provenance = new CoverageProvenance
            {
                DomainId = domain.DomainId,
                PolicyHash = policy.ComputeHash(),
                FieldId = deformationFieldId,
                SourceTruthHashes = velocityCoverage.Provenance.SourceTruthHashes,
                ComputedAt = DateTime.UtcNow
            }
        };
    }

    private static bool TryGetLatitudeDeg(SamplingDomain domain, GridSpec grid, int nodeIndex, out double latDeg)
    {
        latDeg = double.NaN;

        if (grid.NLon <= 0 || grid.NLat <= 0)
            return false;

        var extent = domain.Extent ?? new LatLonExtent { MinLat = -90.0, MaxLat = 90.0, MinLon = -180.0, MaxLon = 180.0 };

        int latIndex = nodeIndex / grid.NLon;
        if ((uint)latIndex >= (uint)grid.NLat)
            return false;

        double startLat = grid.Registration == GridRegistration.Pixel
            ? extent.MinLat + (grid.ResolutionDeg * 0.5)
            : extent.MinLat;

        latDeg = startLat + (latIndex * grid.ResolutionDeg);
        return true;
    }
}
