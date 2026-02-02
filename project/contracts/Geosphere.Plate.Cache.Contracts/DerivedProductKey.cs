using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct DerivedProductKey(
    [property: Key(0)] TruthStreamIdentity Stream,
    [property: Key(1)] string ProductType,
    [property: Key(2)] long LastSequence,
    [property: Key(3)] string PolicyHash)
{
    public string ToInstanceId() => $"{Stream.ToStreamKey()}:{ProductType}:{LastSequence}:{PolicyHash}";
}
