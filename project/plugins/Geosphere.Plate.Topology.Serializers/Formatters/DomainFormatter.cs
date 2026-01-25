using System;
using MessagePack;
using MessagePack.Formatters;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Serializers.Formatters;

/// <summary>
/// Custom MessagePack formatter for Domain type.
/// Domain has a private constructor which requires custom serialization.
/// </summary>
internal class DomainFormatter : IMessagePackFormatter<Domain>
{
    public void Serialize(ref MessagePackWriter writer, Domain value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public Domain Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var strValue = reader.ReadString();
        if (strValue == null)
            throw new InvalidOperationException("Domain value cannot be null");
        return Domain.Parse(strValue);
    }
}
