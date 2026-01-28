using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public sealed record PlatePartitionRegion(
    PlateId PlateId,
    PolygonRegion2 Region);
