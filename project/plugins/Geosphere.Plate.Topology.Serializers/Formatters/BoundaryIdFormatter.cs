using System;
using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for BoundaryId.
/// Encoded as raw Guid (binary).
/// </summary>
internal class BoundaryIdFormatter : IMessagePackFormatter<BoundaryId>
{
    public void Serialize(ref MessagePackWriter writer, BoundaryId value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<Guid>().Serialize(ref writer, value.Value, options);
    }

    public BoundaryId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var guid = options.Resolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, options);
        return new BoundaryId(guid);
    }
}
