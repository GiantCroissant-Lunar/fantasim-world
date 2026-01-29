namespace FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

/// <summary>
/// A raster sequence that applies masking/cookie-cutting to frames.
/// RFC-V2-0028 §3.3 - Derived transform for masking rasters.
/// </summary>
/// <remarks>
/// This interface is domain-agnostic. It does not reference plate polygons or topology.
/// For plate-specific masked sequences, use the composition layer
/// (Geosphere.Plate.Raster.Masking plugin).
/// </remarks>
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
}

/// <summary>
/// Generic factory for creating masked raster sequences.
/// Domain-agnostic - does not reference plates/topology.
/// </summary>
/// <remarks>
/// For plate-specific masking, use <c>IPlateRasterMaskFactory</c> from the
/// Geosphere.Plate.Raster.Masking plugin.
/// </remarks>
public interface IRasterMaskFactory
{
    /// <summary>
    /// Creates a masked sequence using geographic bounds.
    /// </summary>
    IMaskedRasterSequence CreateBoundsMaskedSequence(
        IRasterSequence source,
        RasterBounds bounds);

    /// <summary>
    /// Creates a masked sequence using a custom mask.
    /// </summary>
    IMaskedRasterSequence CreateCustomMaskedSequence(
        IRasterSequence source,
        IRasterMask mask,
        double noDataValue = double.NaN);
}
