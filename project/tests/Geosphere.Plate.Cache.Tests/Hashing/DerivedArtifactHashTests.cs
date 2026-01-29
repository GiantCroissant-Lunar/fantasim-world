using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Hashing;

/// <summary>
/// Test vectors for RFC-V2-0006 Derived Artifact Cache.
///
/// These tests verify that the hash computations produce the expected
/// deterministic outputs as specified in the RFC.
/// </summary>
public class DerivedArtifactHashTests
{
    #region Test Vector 1: Empty Params Hash

    /// <summary>
    /// RFC-V2-0006 §9.1: Empty Params Hash Test Vector
    ///
    /// Input: Empty map {}
    /// Canonical MessagePack: 0x80 (single byte)
    /// Expected SHA-256: 76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71
    /// </summary>
    [Fact]
    public void EmptyParamsHash_ReturnsExpectedValue()
    {
        // Act
        var hash = ParamsHashComputer.Compute(null);

        // Assert
        Assert.Equal("76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71", hash);
    }

    [Fact]
    public void EmptyParamsHash_WithEmptyDictionary_ReturnsExpectedValue()
    {
        // Act
        var hash = ParamsHashComputer.Compute(new Dictionary<string, object?>());

        // Assert
        Assert.Equal("76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71", hash);
    }

    [Fact]
    public void ParamsHashComputer_HasCorrectEmptyParamsHashConstant()
    {
        Assert.Equal("76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71", ParamsHashComputer.EmptyParamsHash);
    }

    #endregion

    #region Test Vector 2: Golden FingerprintEnvelope

    /// <summary>
    /// RFC-V2-0006 §9.2: Golden FingerprintEnvelope Test Vector
    ///
    /// Inputs:
    ///   source_stream     = "S:V1:Bmain:L0:Plates:M0:Events"
    ///   boundary_kind     = "sequence"
    ///   last_sequence     = 0
    ///   generator_id      = "TestGen"
    ///   generator_version = "1.0.0"
    ///   params_hash       = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71"
    ///
    /// Expected array marker: 0x96 (6-element fixarray)
    ///
    /// This test computes the golden fingerprint. The result should be recorded
    /// and used as the expected value in cross-implementation compatibility tests.
    /// </summary>
    [Fact]
    public void GoldenFingerprintEnvelope_ComputesCorrectly()
    {
        // Arrange
        var sourceStream = "S:V1:Bmain:L0:Plates:M0:Events";
        var boundaryKind = "sequence";
        var lastSequence = 0UL;
        var generatorId = "TestGen";
        var generatorVersion = "1.0.0";
        var paramsHash = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71";

        // Act
        var fingerprint = InputFingerprintComputer.Compute(
            sourceStream,
            boundaryKind,
            lastSequence,
            generatorId,
            generatorVersion,
            paramsHash
        );

        // Assert - Verify format (64 lowercase hex characters)
        Assert.Equal(64, fingerprint.Length);
        Assert.All(fingerprint, c => Assert.True(IsLowercaseHexChar(c), $"Character '{c}' is not lowercase hex"));

        // The golden fingerprint is computed deterministically
        // This is the value recorded for the RFC test vector
        Assert.Equal("b22cabf7cd82e2f6a172c1bf11e9e56510a0a084a130fbfbf0a06e05a0d0157e", fingerprint);
    }

    [Fact]
    public void GoldenFingerprintEnvelope_UsingEnvelopeRecord_ComputesCorrectly()
    {
        // Arrange
        var envelope = new FingerprintEnvelope(
            SourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            BoundaryKind: "sequence",
            LastSequence: 0,
            GeneratorId: "TestGen",
            GeneratorVersion: "1.0.0",
            ParamsHash: "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71"
        );

        // Act
        var fingerprint = InputFingerprintComputer.Compute(envelope);

        // Assert
        Assert.Equal(64, fingerprint.Length);
        Assert.Equal("b22cabf7cd82e2f6a172c1bf11e9e56510a0a084a130fbfbf0a06e05a0d0157e", fingerprint);
    }

    [Fact]
    public void FingerprintEnvelope_Encoding_HasCorrectArrayMarker()
    {
        // Arrange
        var sourceStream = "S:V1:Bmain:L0:Plates:M0:Events";
        var boundaryKind = "sequence";
        var lastSequence = 0UL;
        var generatorId = "TestGen";
        var generatorVersion = "1.0.0";
        var paramsHash = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71";

        // Act
        var canonicalBytes = CanonicalMessagePackEncoder.EncodeFingerprintEnvelope(
            sourceStream,
            boundaryKind,
            lastSequence,
            generatorId,
            generatorVersion,
            paramsHash
        );

        // Assert - First byte should be 0x96 (fixarray with 6 elements)
        Assert.NotEmpty(canonicalBytes);
        Assert.Equal(0x96, canonicalBytes[0]);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void ParamsHash_IsDeterministic_SameInputSameOutput()
    {
        // Arrange
        var params1 = new Dictionary<string, object?>
        {
            ["compression"] = "lz4",
            ["snapshot_kind"] = "topology_state_view"
        };

        // Act
        var hash1 = ParamsHashComputer.Compute(params1);
        var hash2 = ParamsHashComputer.Compute(params1);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ParamsHash_KeyOrderIndependent()
    {
        // Arrange - Different insertion order, same keys/values
        var params1 = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = 2
        };
        var params2 = new Dictionary<string, object?>
        {
            ["b"] = 2,
            ["a"] = 1
        };

        // Act
        var hash1 = ParamsHashComputer.Compute(params1);
        var hash2 = ParamsHashComputer.Compute(params2);

        // Assert - Same keys/values should produce same hash regardless of order
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void InputFingerprint_IsDeterministic_SameInputSameOutput()
    {
        // Arrange
        var sourceStream = "S:V1:Bmain:L0:Plates:M0:Events";
        var boundaryKind = "sequence";
        var lastSequence = 42UL;
        var generatorId = "SnapshotterV1";
        var generatorVersion = "1.2.0";
        var paramsHash = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71";

        // Act
        var fingerprint1 = InputFingerprintComputer.Compute(
            sourceStream, boundaryKind, lastSequence,
            generatorId, generatorVersion, paramsHash);
        var fingerprint2 = InputFingerprintComputer.Compute(
            sourceStream, boundaryKind, lastSequence,
            generatorId, generatorVersion, paramsHash);

        // Assert
        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public void InputFingerprint_DifferentFieldsProduceDifferentHashes()
    {
        // Arrange
        var baseParams = new
        {
            SourceStream = "S:V1:Bmain:L0:Plates:M0:Events",
            BoundaryKind = "sequence",
            LastSequence = 0UL,
            GeneratorId = "TestGen",
            GeneratorVersion = "1.0.0",
            ParamsHash = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71"
        };

        var baseFingerprint = InputFingerprintComputer.Compute(
            baseParams.SourceStream, baseParams.BoundaryKind, baseParams.LastSequence,
            baseParams.GeneratorId, baseParams.GeneratorVersion, baseParams.ParamsHash);

        // Act & Assert - Each changed field produces different fingerprint
        var differentStream = InputFingerprintComputer.Compute(
            "S:V2:Bmain:L0:Plates:M0:Events", baseParams.BoundaryKind, baseParams.LastSequence,
            baseParams.GeneratorId, baseParams.GeneratorVersion, baseParams.ParamsHash);
        Assert.NotEqual(baseFingerprint, differentStream);

        var differentSequence = InputFingerprintComputer.Compute(
            baseParams.SourceStream, baseParams.BoundaryKind, 1UL,
            baseParams.GeneratorId, baseParams.GeneratorVersion, baseParams.ParamsHash);
        Assert.NotEqual(baseFingerprint, differentSequence);

        var differentGenerator = InputFingerprintComputer.Compute(
            baseParams.SourceStream, baseParams.BoundaryKind, baseParams.LastSequence,
            "TestGenV2", baseParams.GeneratorVersion, baseParams.ParamsHash);
        Assert.NotEqual(baseFingerprint, differentGenerator);

        var differentVersion = InputFingerprintComputer.Compute(
            baseParams.SourceStream, baseParams.BoundaryKind, baseParams.LastSequence,
            baseParams.GeneratorId, "2.0.0", baseParams.ParamsHash);
        Assert.NotEqual(baseFingerprint, differentVersion);

        var differentParamsHash = InputFingerprintComputer.Compute(
            baseParams.SourceStream, baseParams.BoundaryKind, baseParams.LastSequence,
            baseParams.GeneratorId, baseParams.GeneratorVersion,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        Assert.NotEqual(baseFingerprint, differentParamsHash);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void FingerprintEnvelope_Validate_AcceptsValidData()
    {
        // Arrange
        var envelope = new FingerprintEnvelope(
            SourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            BoundaryKind: "sequence",
            LastSequence: 0,
            GeneratorId: "TestGen",
            GeneratorVersion: "1.0.0",
            ParamsHash: "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71"
        );

        // Act & Assert - Should not throw
        envelope.Validate();
    }

    [Theory]
    [InlineData(null, "sequence", 0UL, "gen", "1.0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("stream", null, 0UL, "gen", "1.0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("stream", "sequence", 0UL, null, "1.0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("stream", "sequence", 0UL, "gen", null, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("stream", "sequence", 0UL, "gen", "1.0", null)]
    [InlineData("stream", "sequence", 0UL, "gen", "1.0", "INVALID")]
    [InlineData("stream", "sequence", 0UL, "gen", "1.0", "INVALIDHASH")]
    [InlineData("stream", "sequence", 0UL, "gen", "1.0", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void FingerprintEnvelope_Validate_RejectsInvalidData(
        string? sourceStream, string? boundaryKind, ulong lastSequence,
        string? generatorId, string? generatorVersion, string? paramsHash)
    {
        // Arrange
        var envelope = new FingerprintEnvelope(
            SourceStream: sourceStream!,
            BoundaryKind: boundaryKind!,
            LastSequence: lastSequence,
            GeneratorId: generatorId!,
            GeneratorVersion: generatorVersion!,
            ParamsHash: paramsHash!
        );

        // Act & Assert - Should throw ArgumentException
        Assert.Throws<ArgumentException>(() => envelope.Validate());
    }

    #endregion

    #region Helper Methods

    private static bool IsLowercaseHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');

    #endregion
}
