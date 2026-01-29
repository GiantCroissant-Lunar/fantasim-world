using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;

namespace FantaSim.Geosphere.Plate.Cache.Tests.TestHelpers;

public sealed class FakeArtifactGenerator : IArtifactGenerator<byte[]>
{
    public FakeArtifactGenerator(string generatorId = "FakeGen", string generatorVersion = "1.0.0")
    {
        GeneratorId = generatorId;
        GeneratorVersion = generatorVersion;
    }

    public string GeneratorId { get; }

    public string GeneratorVersion { get; }

    public Task<byte[]> GenerateAsync(ArtifactGenerationContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var payload = Encoding.UTF8.GetBytes($"payload:{context.InputFingerprint}:{context.LastSequence}");
        return Task.FromResult(payload);
    }

    public byte[] Serialize(byte[] artifact) => artifact;

    public byte[] Deserialize(byte[] data) => data;
}
