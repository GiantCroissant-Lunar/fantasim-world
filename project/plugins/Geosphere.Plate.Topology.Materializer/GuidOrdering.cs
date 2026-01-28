using System;
using System.Collections.Generic;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

public static class GuidOrdering
{
    public static IComparer<Guid> Rfc4122Comparer { get; } = Comparer<Guid>.Create(CompareRfc4122);

    public static int CompareRfc4122(Guid a, Guid b)
    {
        Span<byte> aLe = stackalloc byte[16];
        Span<byte> bLe = stackalloc byte[16];

        a.TryWriteBytes(aLe);
        b.TryWriteBytes(bLe);

        for (var i = 0; i < 16; i++)
        {
            var ab = GetRfc4122ByteAt(aLe, i);
            var bb = GetRfc4122ByteAt(bLe, i);

            if (ab < bb)
                return -1;
            if (ab > bb)
                return 1;
        }

        return 0;
    }

    private static byte GetRfc4122ByteAt(ReadOnlySpan<byte> littleEndianGuidBytes, int index)
    {
        return index switch
        {
            0 => littleEndianGuidBytes[3],
            1 => littleEndianGuidBytes[2],
            2 => littleEndianGuidBytes[1],
            3 => littleEndianGuidBytes[0],
            4 => littleEndianGuidBytes[5],
            5 => littleEndianGuidBytes[4],
            6 => littleEndianGuidBytes[7],
            7 => littleEndianGuidBytes[6],
            _ => littleEndianGuidBytes[index]
        };
    }
}
