using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public sealed record ReconstructableFeature(
    FeatureId FeatureId,
    IGeometry Geometry,
    PlateId? PlateIdProvenance);
