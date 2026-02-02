using System;
using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for JunctionId.
/// Encoded as raw Guid (binary).
/// </summary>
internal sealed class JunctionIdFormatter : IMessagePackFormatter<JunctionId>
{
    public void Serialize(ref MessagePackWriter writer, JunctionId value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<Guid>().Serialize(ref writer, value.Value, options);
    }

    public JunctionId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var guid = options.Resolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, options);
        return new JunctionId(guid);
    }
}
