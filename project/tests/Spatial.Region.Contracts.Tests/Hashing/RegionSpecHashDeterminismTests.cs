using FantaSim.Spatial.Region.Contracts;
using FantaSim.Spatial.Region.Contracts.Hashing;

namespace FantaSim.Spatial.Region.Contracts.Tests.Hashing;

/// <summary>
/// Tests for RegionSpec canonical hash determinism.
/// Per RFC-V2-0055 §8.
/// </summary>
public class RegionSpecHashDeterminismTests
{
    /// <summary>
    /// RFC §8.1: Same inputs produce identical hash.
    /// </summary>
    [Fact]
    public void RegionSpec_SameInputs_ProduceIdenticalHash()
    {
        var region1 = new RegionSpec
        {
            Version = 1,
            Space = "canonical_sphere",
            Shape = new RegionShape
            {
                Kind = "spherical_shell",
                SphericalShell = new SphericalShellParams
                {
                    RMinM = 6371000.0,
                    RMaxM = 6451000.0,
                    AngularClip = null
                }
            },
            Frame = new RegionFrame
            {
                Anchor = new RegionAnchor { Type = "planet_center" },
                Basis = new RegionBasis { Type = "planet_fixed" }
            },
            Sampling = null
        };

        var region2 = new RegionSpec
        {
            Version = 1,
            Space = "canonical_sphere",
            Shape = new RegionShape
            {
                Kind = "spherical_shell",
                SphericalShell = new SphericalShellParams
                {
                    RMinM = 6371000.0,
                    RMaxM = 6451000.0,
                    AngularClip = null
                }
            },
            Frame = new RegionFrame
            {
                Anchor = new RegionAnchor { Type = "planet_center" },
                Basis = new RegionBasis { Type = "planet_fixed" }
            },
            Sampling = null
        };

        var hash1 = RegionSpecHashComputer.ComputeCanonicalHash(region1);
        var hash2 = RegionSpecHashComputer.ComputeCanonicalHash(region2);

        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    /// RFC §8.4: Different regions produce different fingerprints.
    /// </summary>
    [Fact]
    public void DifferentRegions_ProduceDifferentFingerprints()
    {
        var surface = RegionSpec.Surface(thicknessM: 0.0);
        var atmosphere = RegionSpec.SphericalShell(
            rMinM: 6371000.0, rMaxM: 6451000.0);

        var fp1 = RegionSpecHashComputer.ComputeCanonicalHash(surface);
        var fp2 = RegionSpecHashComputer.ComputeCanonicalHash(atmosphere);

        Assert.NotEqual(fp1, fp2);
    }

    /// <summary>
    /// Hash is 64-character lowercase hex (SHA-256).
    /// </summary>
    [Fact]
    public void Hash_Is64CharLowercaseHex()
    {
        var region = RegionSpec.Surface(thicknessM: 0.0);

        var hash = RegionSpecHashComputer.ComputeCanonicalHash(region);

        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'),
            $"Character '{c}' is not lowercase hex"));
    }

    /// <summary>
    /// Multiple identical calls produce same hash.
    /// </summary>
    [Fact]
    public void Repeatability_MultipleCalls()
    {
        var region = RegionSpec.SphericalShell(5711000.0, 6336000.0);

        var hashes = new[]
        {
            RegionSpecHashComputer.ComputeCanonicalHash(region),
            RegionSpecHashComputer.ComputeCanonicalHash(region),
            RegionSpecHashComputer.ComputeCanonicalHash(region)
        };

        Assert.All(hashes, hash => Assert.Equal(hashes[0], hash));
    }
}
