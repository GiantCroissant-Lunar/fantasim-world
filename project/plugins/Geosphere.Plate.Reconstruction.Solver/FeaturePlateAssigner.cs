using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
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
            .OrderBy(p => p.PlateId.Value, GuidOrdering.Rfc4122Comparer)
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

}
