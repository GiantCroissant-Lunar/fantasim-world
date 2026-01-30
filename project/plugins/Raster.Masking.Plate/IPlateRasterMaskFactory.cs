using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Raster.Masking.Plate;

/// <summary>
/// Plate-specific factory for creating masked raster sequences.
/// RFC-V2-0028 §8.2.2 - Composition layer that bridges raster + plates domains.
/// </summary>
/// <remarks>
/// This interface lives in the Masking plugin (composition layer), not in
/// Raster.Contracts, to maintain domain-agnostic contracts.
/// </remarks>
public interface IPlateRasterMaskFactory : IRasterMaskFactory
{
    /// <summary>
    /// Creates a masked sequence using a plate polygon set.
    /// Pixels inside plate polygons are kept; others are masked out.
    /// </summary>
    /// <param name="source">The source raster sequence.</param>
    /// <param name="polygonSet">The plate polygon set for masking.</param>
    /// <param name="specificPlates">Optional: limit masking to specific plates. If null, all plates are included.</param>
    /// <returns>A masked raster sequence.</returns>
    IPlateMaskedRasterSequence CreatePlateMaskedSequence(
        IRasterSequence source,
        PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null);
}

/// <summary>
/// Extended masked raster sequence that includes plate polygon information.
/// RFC-V2-0028 §8.2.2 - Composition layer interface.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IMaskedRasterSequence"/> with plate-specific
/// properties. It lives in the Masking plugin, not in Raster.Contracts.
/// </remarks>
public interface IPlateMaskedRasterSequence : IMaskedRasterSequence
{
    /// <summary>
    /// The plate polygon set used for masking.
    /// </summary>
    PlatePolygonSet PolygonSet { get; }

    /// <summary>
    /// The specific plates included in the mask (null = all plates).
    /// </summary>
    IReadOnlyCollection<PlateId>? SpecificPlates { get; }
}

/// <summary>
/// Mask specification for plate polygon-based masking.
/// RFC-V2-0028 - Plate-specific masking configuration.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlatePolygonMaskSpec(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] bool IncludeInterior,
    [property: Key(2)] double BufferDegrees
)
{
    /// <summary>
    /// Mask that includes only the interior of the polygon.
    /// </summary>
    public static PlatePolygonMaskSpec Interior(PlateId plateId, double bufferDegrees = 0)
        => new(plateId, true, bufferDegrees);

    /// <summary>
    /// Mask that excludes the interior (keeps exterior).
    /// </summary>
    public static PlatePolygonMaskSpec Exterior(PlateId plateId, double bufferDegrees = 0)
        => new(plateId, false, bufferDegrees);
}
