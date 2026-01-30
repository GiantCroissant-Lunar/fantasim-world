using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Sensitivity;

public class SensitivityTests
{
    [Fact]
    public void VersionChange_DifferentFingerprint()
    {
        var paramsHash = ParamsHashComputer.EmptyParamsHash;
        var baseFingerprint = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            paramsHash);

        var changed = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "2.0.0",
            paramsHash);

        Assert.NotEqual(baseFingerprint, changed);
    }

    [Fact]
    public void ParamsChange_DifferentFingerprint()
    {
        var baseFingerprint = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            ParamsHashComputer.EmptyParamsHash);

        var changedParams = ParamsHashComputer.Compute(new Dictionary<string, object?> { ["a"] = 1 });
        var changedFingerprint = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            changedParams);

        Assert.NotEqual(baseFingerprint, changedFingerprint);
    }

    [Fact]
    public void SequenceChange_DifferentFingerprint()
    {
        var paramsHash = ParamsHashComputer.EmptyParamsHash;
        var baseFingerprint = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            paramsHash);

        var changed = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            1,
            "TestGen",
            "1.0.0",
            paramsHash);

        Assert.NotEqual(baseFingerprint, changed);
    }

    [Fact]
    public void SourceStreamChange_DifferentFingerprint()
    {
        var paramsHash = ParamsHashComputer.EmptyParamsHash;
        var baseFingerprint = InputFingerprintComputer.Compute(
            "S:V1:Bmain:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            paramsHash);

        var changed = InputFingerprintComputer.Compute(
            "S:V1:Bdev:L0:Plates:M0:Events",
            "sequence",
            0,
            "TestGen",
            "1.0.0",
            paramsHash);

        Assert.NotEqual(baseFingerprint, changed);
    }

    [Fact]
    public void BoundaryKindChange_DifferentFingerprint()
    {
        var fingerprintSequence = ComputeFingerprint("sequence");
        var fingerprintTime = ComputeFingerprint("time");

        Assert.NotEqual(fingerprintSequence, fingerprintTime);
    }

    private static string ComputeFingerprint(string boundaryKind)
    {
        var canonicalBytes = DerivedCacheCanonicalMessagePackEncoder.EncodeFingerprintEnvelope(
            sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            boundaryKind: boundaryKind,
            lastSequence: 0,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: ParamsHashComputer.EmptyParamsHash);

        var hashBytes = SHA256.HashData(canonicalBytes);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
