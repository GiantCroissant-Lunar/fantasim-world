using System.Buffers;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Serialization;

public static class DerivedProductCanonicalMessagePackEncoder
{
    public static byte[] EncodeSliceIdentity(
        string streamUrn,
        long tick,
        long sliceLastSequence,
        int schemaVersion)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteArrayHeader(4);
        writer.Write(streamUrn);
        writer.Write(tick);
        writer.Write(sliceLastSequence);
        writer.Write(schemaVersion);

        writer.Flush();
        return buffer.WrittenMemory.ToArray();
    }

    public static byte[] EncodeTopologySliceIdentity(TopologySliceIdentity identity)
    {
        return EncodeSliceIdentity(
            streamUrn: identity.Stream.ToString(),
            tick: identity.Tick.Value,
            sliceLastSequence: identity.SliceLastSequence,
            schemaVersion: identity.SchemaVersion);
    }

    public static byte[] EncodeKinematicsSliceIdentity(KinematicsSliceIdentity identity)
    {
        return EncodeSliceIdentity(
            streamUrn: identity.Stream.ToString(),
            tick: identity.Tick.Value,
            sliceLastSequence: identity.SliceLastSequence,
            schemaVersion: identity.SchemaVersion);
    }
}
