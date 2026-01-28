using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;
using UnifyGeometry.Operations;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

public sealed class FeaturePlateAssigner : IFeaturePlateAssigner
{
    public IReadOnlyList<ReconstructableFeature> AssignPlateProvenance(
        IReadOnlyList<ReconstructableFeature> features,
        IReadOnlyList<PlatePartitionRegion> partition)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(partition);

        var sortedPartition = partition
            .Where(p => p is not null)
            .OrderBy(p => p.PlateId.Value, Rfc4122GuidComparer.Instance)
            .ToArray();

        var output = new List<ReconstructableFeature>(features.Count);

        for (var i = 0; i < features.Count; i++)
        {
            var f = features[i];

            if (f.PlateIdProvenance.HasValue)
            {
                output.Add(f);
                continue;
            }

            if (!TryGetRepresentativePoint(f.Geometry, out var point))
            {
                output.Add(f);
                continue;
            }

            PlateId? assigned = null;
            for (var j = 0; j < sortedPartition.Length; j++)
            {
                var region = sortedPartition[j].Region;
                if (PolygonRegion2Ops.ContainsPoint(region, point))
                {
                    assigned = sortedPartition[j].PlateId;
                    break;
                }
            }

            output.Add(f with { PlateIdProvenance = assigned });
        }

        return output;
    }

    private static bool TryGetRepresentativePoint(IGeometry geometry, out Point2 point)
    {
        switch (geometry)
        {
            case Point2 p:
                point = p;
                return !p.IsEmpty;
            case Polyline2 line when line.Count > 0:
                point = line[0];
                return !point.IsEmpty;
            default:
                point = Point2.Empty;
                return false;
        }
    }

    private sealed class Rfc4122GuidComparer : IComparer<Guid>
    {
        public static Rfc4122GuidComparer Instance { get; } = new();

        public int Compare(Guid x, Guid y)
        {
            Span<byte> aLe = stackalloc byte[16];
            Span<byte> bLe = stackalloc byte[16];

            if (!x.TryWriteBytes(aLe))
                throw new InvalidOperationException("Failed to write Guid bytes.");
            if (!y.TryWriteBytes(bLe))
                throw new InvalidOperationException("Failed to write Guid bytes.");

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
}
