using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

/// <summary>
/// A raster sequence that applies masking/cookie-cutting to frames.
/// RFC-V2-0028 §3.3 - Derived transform for masking rasters by reconstructed polygons.
/// </summary>
public interface IMaskedRasterSequence : IRasterSequence
{
    /// <summary>
    /// The underlying (unmasked) raster sequence.
    /// </summary>
    IRasterSequence SourceSequence { get; }

    /// <summary>
    /// The mask applied to each frame.
    /// </summary>
    IRasterMask Mask { get; }

    /// <summary>
    /// The plate polygon set used for masking (if plate-based masking).
    /// Null for other mask types.
    /// </summary>
    FantaSim.Geosphere.Plate.Polygonization.Contracts.Products.PlatePolygonSet? PolygonSet { get; }
}

/// <summary>
/// Factory for creating masked raster sequences.
/// </summary>
public interface IRasterMaskFactory
{
    /// <summary>
    /// Creates a masked sequence using a plate polygon set.
    /// Pixels inside plate polygons are kept; others are masked out.
    /// </summary>
    IMaskedRasterSequence CreatePlateMaskedSequence(
        IRasterSequence source,
        FantaSim.Geosphere.Plate.Polygonization.Contracts.Products.PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null);

    /// <summary>
    /// Creates a masked sequence using geographic bounds.
    /// </summary>
    IMaskedRasterSequence CreateBoundsMaskedSequence(
        IRasterSequence source,
        RasterBounds bounds);
}
