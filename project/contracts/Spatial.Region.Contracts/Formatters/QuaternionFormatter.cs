using MessagePack;
using MessagePack.Formatters;

namespace FantaSim.Spatial.Region.Contracts.Formatters;

/// <summary>
/// MessagePack formatter for our custom Quaternion type.
/// Serializes as compact array [w, x, y, z].
/// </summary>
public sealed class RegionQuaternionFormatter : IMessagePackFormatter<Quaternion>
{
    public void Serialize(ref MessagePackWriter writer, Quaternion value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.W);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    public Quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 4)
        {
            throw new MessagePackSerializationException($"Invalid Quaternion format. Expected 4 elements but got {length}");
        }

        var w = reader.ReadDouble();
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var z = reader.ReadDouble();
        return new Quaternion(w, x, y, z);
    }
}
