using System;
using FantaSim.Geosphere.Plate.Deformation.Contracts;
using FantaSim.Geosphere.Plate.Deformation.Solver;
using FantaSim.Geosphere.Plate.Sampling.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Deformation.Tests;

public sealed class DeformationTestGates
{
    [Fact]
    public void RigidRotation_ZeroStrainRate() // RFC-V2-0054 §10.1
    {
        // Setup
        var grid = new GridSpec { NLon = 360, NLat = 180, Registration = GridRegistration.Gridline, ResolutionDeg = 1.0 }; // 1 degree grid
        var domain = new SamplingDomain { DomainId = "test-domain", Grid = grid, DomainType = SamplingDomainType.Regular, Extent = new LatLonExtent { MinLon = -180, MaxLon = 180, MinLat = -90, MaxLat = 90 } };
        var tick = new CanonicalTick(0);
        var policy = new ReconstructionPolicy { Frame = MantleFrame.Instance, KinematicsModel = ModelId.Default, PartitionTolerance = new TolerancePolicy.StrictPolicy() };

        // Rigid Rotation: V_east = R * w * cos(lat), V_north = 0.
        // Let R=1, w=0.01.
        double w = 0.01;
        var service = new StubSamplingService((latRad, lonRad) =>
        {
            double ve = 1.0 * w * Math.Cos(latRad);
            double vn = 0.0;
            return (ve, vn);
        });

        var solver = new DeformationSolver(service);
        var strainRate = solver.ComputeStrainRate(domain, tick, policy);

        int validCount = 0;
        for (int i = 0; i < strainRate.Tensors.Length; i++)
        {
            // Skip poles
            if (IsPole(grid, i)) continue;

            var tensor = strainRate.Tensors[i];

            // Check for NaN propagation
            if (double.IsNaN(tensor.Eee)) continue;

            validCount++;
            Assert.True(Math.Abs(tensor.Eee) < 1e-5, $"Eee should be 0, got {tensor.Eee} at index {i}");
            Assert.True(Math.Abs(tensor.Enn) < 1e-5, $"Enn should be 0, got {tensor.Enn} at index {i}");
            Assert.True(Math.Abs(tensor.Een) < 1e-5, $"Een should be 0, got {tensor.Een} at index {i}");
            Assert.True(Math.Abs(tensor.DilatationRate) < 1e-5, "Dilatation should be 0");
        }
        Assert.True(validCount > 0);
    }

    [Fact]
    public void PureDivergence_PositiveDilatation_ZeroVorticity() // RFC-V2-0054 §10.2
    {
        // Setup
        var grid = new GridSpec { NLon = 360, NLat = 180, Registration = GridRegistration.Gridline, ResolutionDeg = 1.0 };
        var domain = new SamplingDomain { DomainId = "test-domain", Grid = grid, DomainType = SamplingDomainType.Regular, Extent = new LatLonExtent { MinLon = -180, MaxLon = 180, MinLat = -90, MaxLat = 90 } };
        var tick = new CanonicalTick(0);
        var policy = new ReconstructionPolicy { Frame = MantleFrame.Instance, KinematicsModel = ModelId.Default, PartitionTolerance = new TolerancePolicy.StrictPolicy() };

        // Constant Southward Flow: Vn = -0.01.
        // Div = (1/R) tan(lat). Positive in N hemisphere.
        // Vorticity = 0.
        var service = new StubSamplingService((latRad, lonRad) =>
        {
            return (0.0, -0.01);
        });

        var solver = new DeformationSolver(service);

        var dilatation = solver.ComputeScalarField(domain, DeformationFieldId.DilatationRate, tick, policy);
        var vorticity = solver.ComputeScalarField(domain, DeformationFieldId.Vorticity, tick, policy);

        int validCount = 0;
        int nLon = grid.NLon;

        for (int j = 0; j < grid.NLat; j++)
        {
            double latDeg = -90.0 + j * 1.0;
            if (latDeg <= 10.0 || latDeg >= 80.0) continue; // Check checks in Northern Hemisphere, avoid equator/poles

            for (int i = 0; i < nLon; i++)
            {
                 int idx = j * nLon + i;
                 double d = dilatation.Values[idx];
                 double v = vorticity.Values[idx];

                 Assert.True(d > 0, $"Dilatation should be positive at lat {latDeg}, got {d}");
                 Assert.True(Math.Abs(v) < 1e-9, $"Vorticity should be 0 at lat {latDeg}, got {v}");
                 validCount++;
            }
        }
        Assert.True(validCount > 0);
    }

    [Fact]
    public void SecondInvariant_AlwaysNonNegative() // RFC-V2-0054 §10.3
    {
        // Random field
        var grid = new GridSpec { NLon = 360, NLat = 180, Registration = GridRegistration.Gridline, ResolutionDeg = 1.0 };
        var domain = new SamplingDomain { DomainId = "test-domain", Grid = grid, DomainType = SamplingDomainType.Regular, Extent = new LatLonExtent { MinLon = -180, MaxLon = 180, MinLat = -90, MaxLat = 90 } };
        var service = new StubSamplingService((lat, lon) => (Math.Sin(lon*5), Math.Cos(lat*3))); // complex flow
        var solver = new DeformationSolver(service);

        var field = solver.ComputeScalarField(domain, DeformationFieldId.SecondInvariant, new CanonicalTick(0), new ReconstructionPolicy { Frame = MantleFrame.Instance, KinematicsModel = ModelId.Default, PartitionTolerance = new TolerancePolicy.StrictPolicy() });

        foreach (var val in field.Values)
        {
            if (double.IsNaN(val)) continue;
            Assert.True(val >= 0);
        }
    }

    [Fact]
    public void DivergenceConvergence_PartitionsDilatation() // RFC-V2-0054 §10.4
    {
        var grid = new GridSpec { NLon = 36, NLat = 18, Registration = GridRegistration.Gridline, ResolutionDeg = 10.0 };
        var domain = new SamplingDomain { DomainId = "test-domain", Grid = grid, DomainType = SamplingDomainType.Regular, Extent = new LatLonExtent { MinLon = -180, MaxLon = 180, MinLat = -90, MaxLat = 90 } };
        var service = new StubSamplingService((lat, lon) => (Math.Sin(lon), Math.Cos(lat)));
        var solver = new DeformationSolver(service);
        var tick = new CanonicalTick(0);
        var policy = new ReconstructionPolicy { Frame = MantleFrame.Instance, KinematicsModel = ModelId.Default, PartitionTolerance = new TolerancePolicy.StrictPolicy() };

        var dilatation = solver.ComputeScalarField(domain, DeformationFieldId.DilatationRate, tick, policy);
        var divergence = solver.ComputeScalarField(domain, DeformationFieldId.Divergence, tick, policy);
        var convergence = solver.ComputeScalarField(domain, DeformationFieldId.Convergence, tick, policy);

        for (int i = 0; i < dilatation.Values.Length; i++)
        {
            double d = dilatation.Values[i];
            if (double.IsNaN(d)) continue;

            Assert.Equal(Math.Max(d, 0), divergence.Values[i], 1e-9);
            Assert.Equal(Math.Max(-d, 0), convergence.Values[i], 1e-9);
        }
    }

    [Fact]
    public void StrainRate_NaN_AtPoles() // RFC-V2-0054 §10.5
    {
        var grid = new GridSpec { NLon = 360, NLat = 181, Registration = GridRegistration.Gridline, ResolutionDeg = 1.0 }; // Includes +90 and -90
        var domain = new SamplingDomain { DomainId = "test-domain", Grid = grid, DomainType = SamplingDomainType.Regular, Extent = new LatLonExtent { MinLon = -180, MaxLon = 180, MinLat = -90, MaxLat = 90 } };
        var service = new StubSamplingService((lat, lon) => (1, 1));
        var solver = new DeformationSolver(service);

        var strainRate = solver.ComputeStrainRate(domain, new CanonicalTick(0), new ReconstructionPolicy { Frame = MantleFrame.Instance, KinematicsModel = ModelId.Default, PartitionTolerance = new TolerancePolicy.StrictPolicy() });

        // Index 0 (South Pole) and Last Index (North Pole)?
        // -90 is index 0 (row 0). +90 is index 180 (row 180).

        bool foundPole = false;
        int nLon = grid.NLon;

        // Check South Pole Row
        for(int i=0; i<nLon; i++)
        {
            Assert.True(double.IsNaN(strainRate.Tensors[i].Eee));
            foundPole = true;
        }

        // Check North Pole Row
        int startNorth = 180 * nLon;
        for(int i=0; i<nLon; i++)
        {
             Assert.True(double.IsNaN(strainRate.Tensors[startNorth + i].Eee));
        }

        Assert.True(foundPole);
    }

    private bool IsPole(GridSpec grid, int index)
    {
        int nLon = grid.NLon;
        int row = index / nLon;
        // Simple check for start/end rows if they correspond to -90/90
        // Gridline registration: Row 0 is -90, Row NLat-1 is +90.
        // Pixel: Row 0 is -90 + dy/2.

        // This helper is approximate, relying on specific grid specs in tests.
        // For 180 lat segments gridline: 0 and 180 (if NLat=181).
        // If NLat=180, MaxLat = -90 + 179*1 = 89. No pole?
        // Wait, 180 segments -> 181 points for gridline.
        // My tests use NLat=180. -90...89?
        // Let's check logic: lat = min + j*res.
        // If NLat=180, res=1. -90..89.
        // So South Pole is included (j=0).
        if (row == 0) return true;
        if (row == grid.NLat - 1)
        {
             double lat = -90.0 + row * grid.ResolutionDeg;
             if (Math.Abs(lat - 90.0) < 1e-9) return true;
        }
        return false;
    }
}

// Stub implementation
public class StubSamplingService : ISamplingService
{
    private readonly Func<double, double, (double Ve, double Vn)> _velocityFunc;

    public StubSamplingService(Func<double, double, (double Ve, double Vn)> velocityFunc)
    {
        _velocityFunc = velocityFunc;
    }

    public VectorCoverage SampleVectorField(SamplingDomain domain, VectorFieldId fieldId, CanonicalTick tick, ReconstructionPolicy policy)
    {
        var grid = domain.Grid!;
        int count = grid.NodeCount;
        var comps = new double[count * 2];

        for (int j = 0; j < grid.NLat; j++)
        {
            double latDeg = domain.Extent!.MinLat + j * grid.ResolutionDeg; // Simplified
            if (grid.Registration == GridRegistration.Pixel) latDeg += grid.ResolutionDeg * 0.5;

            double latRad = latDeg * Math.PI / 180.0;

            for (int i = 0; i < grid.NLon; i++)
            {
                 // Lon should be -180 .. 180?
                 double lonDeg = domain.Extent!.MinLon + i * grid.ResolutionDeg;
                 if (grid.Registration == GridRegistration.Pixel) lonDeg += grid.ResolutionDeg * 0.5;
                 double lonRad = lonDeg * Math.PI / 180.0;

                 var (ve, vn) = _velocityFunc(latRad, lonRad);
                 int idx = j * grid.NLon + i;
                 comps[idx * 2] = ve;
                 comps[idx * 2 + 1] = vn;
            }
        }

        return new VectorCoverage
        {
            Domain = domain,
            Tick = tick,
            FieldId = fieldId,
            Components = comps,
            Provenance = new CoverageProvenance { DomainId=domain.DomainId, PolicyHash=policy.ComputeHash(), FieldId="stub", SourceTruthHashes=Array.Empty<string>(), ComputedAt=DateTime.UtcNow }
        };
    }

    public ScalarCoverage SampleScalarField(SamplingDomain domain, ScalarFieldId fieldId, CanonicalTick tick, ReconstructionPolicy policy)
    {
        throw new NotImplementedException();
    }
}
