using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Determinism;

public class InputFingerprintDeterminismTests
{
    [Fact]
    public void GoldenFingerprint_MatchesExpected()
    {
        var fingerprint = InputFingerprintComputer.ComputeGoldenFingerprint();

        Assert.Equal("b22cabf7cd82e2f6a172c1bf11e9e56510a0a084a130fbfbf0a06e05a0d0157e", fingerprint);
    }

    [Fact]
    public void SameInputs_SameFingerprint()
    {
        var envelope = new FingerprintEnvelope(
            SourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            BoundaryKind: "sequence",
            LastSequence: 7,
            GeneratorId: "TestGen",
            GeneratorVersion: "1.0.0",
            ParamsHash: ParamsHashComputer.EmptyParamsHash);

        var fingerprint1 = InputFingerprintComputer.Compute(envelope);
        var fingerprint2 = InputFingerprintComputer.Compute(envelope);

        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ArrayEncoding_Used()
    {
        var canonicalBytes = DerivedCacheCanonicalMessagePackEncoder.EncodeFingerprintEnvelope(
            sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            boundaryKind: "sequence",
            lastSequence: 0,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: ParamsHashComputer.EmptyParamsHash);

        Assert.NotEmpty(canonicalBytes);
        Assert.Equal(0x96, canonicalBytes[0]);
    }

    [Fact]
    public void FieldOrder_Significant()
    {
        var fingerprintA = InputFingerprintComputer.Compute(
            sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            boundaryKind: "sequence",
            lastSequence: 0,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: ParamsHashComputer.EmptyParamsHash);

        var fingerprintB = InputFingerprintComputer.Compute(
            sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            boundaryKind: "sequence",
            lastSequence: 0,
            generatorId: "1.0.0",
            generatorVersion: "TestGen",
            paramsHash: ParamsHashComputer.EmptyParamsHash);

        Assert.NotEqual(fingerprintA, fingerprintB);
    }

    [Fact]
    public void Repeatability_MultipleCalls()
    {
        var hashes = new[]
        {
            InputFingerprintComputer.Compute(
                sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
                boundaryKind: "sequence",
                lastSequence: 5,
                generatorId: "TestGen",
                generatorVersion: "1.0.0",
                paramsHash: ParamsHashComputer.EmptyParamsHash),
            InputFingerprintComputer.Compute(
                sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
                boundaryKind: "sequence",
                lastSequence: 5,
                generatorId: "TestGen",
                generatorVersion: "1.0.0",
                paramsHash: ParamsHashComputer.EmptyParamsHash),
            InputFingerprintComputer.Compute(
                sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
                boundaryKind: "sequence",
                lastSequence: 5,
                generatorId: "TestGen",
                generatorVersion: "1.0.0",
                paramsHash: ParamsHashComputer.EmptyParamsHash)
        };

        Assert.All(hashes, hash => Assert.Equal(hashes[0], hash));
    }
}
