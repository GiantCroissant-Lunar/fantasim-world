namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IDerivedProductGenerator<T>
{
    string GeneratorId { get; }

    string GeneratorVersion { get; }

    Task<T> ComputeAsync(CancellationToken ct);

    byte[] Serialize(T product);

    T Deserialize(byte[] data);
}
