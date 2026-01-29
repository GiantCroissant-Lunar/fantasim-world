namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IArtifactGenerator<T>
{
    string GeneratorId { get; }

    string GeneratorVersion { get; }

    Task<T> GenerateAsync(ArtifactGenerationContext context, CancellationToken ct);

    byte[] Serialize(T artifact);

    T Deserialize(byte[] data);
}
