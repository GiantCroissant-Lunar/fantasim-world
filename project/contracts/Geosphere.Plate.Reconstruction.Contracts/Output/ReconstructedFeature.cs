using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

public readonly record struct ReconstructedFeature(
    FeatureId FeatureId,
    PlateId PlateIdProvenance,
    IGeometry Geometry);
