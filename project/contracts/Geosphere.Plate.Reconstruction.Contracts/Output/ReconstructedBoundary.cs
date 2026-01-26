using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

/// <summary>
/// Reconstructed boundary geometry at a target tick.
///
/// Normative: each output element carries a single PlateId provenance (RFC-V2-0024 §4).
/// </summary>
public readonly record struct ReconstructedBoundary(
    BoundaryId BoundaryId,
    PlateId PlateIdProvenance,
    IGeometry Geometry);
