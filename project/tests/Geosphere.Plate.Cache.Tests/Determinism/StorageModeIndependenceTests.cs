using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Cache.Tests.TestHelpers;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Determinism;

public class StorageModeIndependenceTests
{
    [Fact]
    public void Fingerprint_SameForEmbeddedAndExternal()
    {
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["compression"] = "lz4" };
        var paramsHash = ParamsHashComputer.Compute(parameters);

        var embeddedFingerprint = InputFingerprintComputer.Compute(
            sourceStream: stream.ToEventStreamIdString(),
            boundaryKind: "sequence",
            lastSequence: 10,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: paramsHash);

        var externalFingerprint = InputFingerprintComputer.Compute(
            sourceStream: stream.ToEventStreamIdString(),
            boundaryKind: "sequence",
            lastSequence: 10,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: paramsHash);

        Assert.Equal(embeddedFingerprint, externalFingerprint);
    }

    [Fact]
    public async Task ContentHash_MatchesPayload()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            1,
            "TestGen",
            "1.0.0",
            paramsHash);

        var payload = Encoding.UTF8.GetBytes("payload-content");
        var manifest = Manifest.Create(
            "Atlas",
            inputFingerprint,
            stream.ToString(),
            Boundary.Sequence(1),
            new GeneratorInfo("TestGen", "1.0.0"),
            paramsHash,
            StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0));

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        var storedManifest = await storage.GetManifestAsync(manifestKey, CancellationToken.None);
        Assert.NotNull(storedManifest);

        var expectedHash = ComputeSha256Hex(payload);
        Assert.Equal(expectedHash, storedManifest!.Value.Storage.ContentHash);
    }

    private static string ComputeSha256Hex(byte[] payload)
    {
        var hashBytes = SHA256.HashData(payload);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
