using MessagePack;
using MessagePack.Formatters;

namespace FantaSim.Spatial.Region.Contracts.Formatters;

/// <summary>
/// MessagePack formatter for Vec3 type.
/// </summary>
public sealed class Vec3Formatter : IMessagePackFormatter<Vec3>
{
    public void Serialize(ref MessagePackWriter writer, Vec3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    public Vec3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 3)
        {
            throw new MessagePackSerializationException($"Invalid Vec3 format. Expected 3 elements but got {length}");
        }

        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();
        return new Vec3(x, y, z);
    }
}
