using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Integration;

public class GeneratorPatternTests
{
    [Fact]
    public async Task PlateAdjacencyArtifactGenerator_SerializationRoundTrip()
    {
        var generator = new PlateAdjacencyArtifactGenerator();
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var context = new ArtifactGenerationContext(stream, 5, "fp");

        var artifact = await generator.GenerateAsync(context, CancellationToken.None);
        var serialized = generator.Serialize(artifact);
        var deserialized = generator.Deserialize(serialized);

        Assert.Equal(artifact, deserialized);
    }

    private sealed class PlateAdjacencyArtifactGenerator : IArtifactGenerator<PlateAdjacencyArtifact>
    {
        public string GeneratorId => "PlateAdjacency";

        public string GeneratorVersion => "1.0.0";

        public Task<PlateAdjacencyArtifact> GenerateAsync(ArtifactGenerationContext context, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var adjacency = $"0:{context.LastSequence};1:{context.LastSequence + 1};2:{context.LastSequence + 2}";
            return Task.FromResult(new PlateAdjacencyArtifact(adjacency));
        }

        public byte[] Serialize(PlateAdjacencyArtifact artifact) => Encoding.UTF8.GetBytes(artifact.Adjacency);

        public PlateAdjacencyArtifact Deserialize(byte[] data) => new(Encoding.UTF8.GetString(data));
    }

    private sealed record PlateAdjacencyArtifact(string Adjacency);
}
