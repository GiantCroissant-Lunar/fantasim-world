using MessagePack;
using MessagePack.Resolvers;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

/// <summary>
/// MessagePack serializer for Manifest records.
/// </summary>
public static class ManifestSerializer
{
    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IFormatterResolver[]
            {
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance
            }));

    public static byte[] Serialize(Manifest manifest)
    {
        return MessagePackSerializer.Serialize(manifest, Options);
    }

    public static Manifest Deserialize(byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<Manifest>(bytes, Options);
    }
}
