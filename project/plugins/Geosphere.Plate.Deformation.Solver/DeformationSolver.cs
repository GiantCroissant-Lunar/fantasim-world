using System;
using FantaSim.Geosphere.Plate.Deformation.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Sampling.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Deformation.Solver;

public sealed class DeformationSolver : IDeformationService
{
    public StrainRateCoverage ComputeStrainRate(
        SamplingDomain domain,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        if (domain.Grid is null)
            throw new NotSupportedException("Only regular grid sampling domains are supported (SamplingDomain.Grid must be non-null).");

        var grid = domain.Grid;
        int nodeCount = grid.NodeCount;

        var tensors = new StrainRateTensor[nodeCount];

        // Minimal viable implementation:
        // - Return zero strain everywhere (rigid motion baseline)
        // - Preserve pole behavior (NaN at +/-90 deg) when grid includes poles
        for (int i = 0; i < nodeCount; i++)
        {
            double latDeg = TryGetLatitudeDeg(domain, grid, i, out var lat) ? lat : double.NaN;

            bool isPole = !double.IsNaN(latDeg) && (Math.Abs(latDeg - 90.0) < 1e-12 || Math.Abs(latDeg + 90.0) < 1e-12);
            double value = isPole ? double.NaN : 0.0;

            tensors[i] = new StrainRateTensor
            {
                Eee = value,
                Enn = value,
                Een = value
            };
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
                VelocityCoverageId = "stub",
                DifferentiationScheme = "zero-strain-stub",
                SourceTruthHashes = Array.Empty<string>(),
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

        var strainRate = ComputeStrainRate(domain, tick, policy);
        int nodeCount = strainRate.Tensors.Length;

        var values = new double[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            var tensor = strainRate.Tensors[i];

            bool anyNaN = double.IsNaN(tensor.Eee) || double.IsNaN(tensor.Enn) || double.IsNaN(tensor.Een);
            if (anyNaN)
            {
                values[i] = double.NaN;
                continue;
            }

            values[i] = deformationFieldId switch
            {
                DeformationFieldId.DilatationRate => tensor.DilatationRate,
                DeformationFieldId.SecondInvariant => tensor.SecondInvariant,
                DeformationFieldId.Vorticity => 0.0,
                DeformationFieldId.Divergence => Math.Max(tensor.DilatationRate, 0.0),
                DeformationFieldId.Convergence => Math.Max(-tensor.DilatationRate, 0.0),
                _ => throw new ArgumentException($"Unknown deformation field ID: '{deformationFieldId}'", nameof(deformationFieldId))
            };
        }

        return new ScalarCoverage
        {
            Domain = domain,
            Tick = tick,
            // Sampling.Contracts currently defines ScalarFieldId as an enum; provenance carries the deformation field id string.
            FieldId = ScalarFieldId.SpeedMagnitude,
            Values = values,
            Provenance = new CoverageProvenance
            {
                DomainId = domain.DomainId,
                PolicyHash = policy.ComputeHash(),
                FieldId = deformationFieldId,
                SourceTruthHashes = strainRate.Provenance.SourceTruthHashes,
                ComputedAt = strainRate.Provenance.ComputedAt
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
