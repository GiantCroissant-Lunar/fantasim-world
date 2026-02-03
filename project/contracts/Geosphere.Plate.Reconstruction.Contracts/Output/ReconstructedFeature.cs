using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

[MessagePackObject]
public readonly record struct ReconstructedFeature(
    [property: Key(0)] FeatureId FeatureId,
    [property: Key(1)] PlateId PlateIdProvenance,
    [property: Key(2)] IGeometry Geometry);
