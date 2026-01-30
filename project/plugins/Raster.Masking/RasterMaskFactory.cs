using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Raster.Core;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Raster.Masking;

/// <summary>
/// Factory for creating masked raster sequences.
/// RFC-V2-0028 §8.2.2 compliant - composition layer that bridges raster + plates domains.
/// </summary>
/// <remarks>
/// Implements both domain-agnostic <see cref="IRasterMaskFactory"/> and
/// plate-specific <see cref="IPlateRasterMaskFactory"/>.
/// </remarks>
public sealed class RasterMaskFactory : IPlateRasterMaskFactory
{
    private const double DefaultNoDataValue = double.NaN;

    /// <inheritdoc />
    public IPlateMaskedRasterSequence CreatePlateMaskedSequence(
        IRasterSequence source,
        PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var mask = new PlatePolygonRasterMask(polygonSet, specificPlates);
        return new PlateMaskedRasterSequence(source, mask, DefaultNoDataValue, polygonSet, specificPlates);
    }

    /// <inheritdoc />
    public IMaskedRasterSequence CreateBoundsMaskedSequence(
        IRasterSequence source,
        RasterBounds bounds)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var mask = new Core.BoundsRasterMask(bounds, includeInterior: true);
        return new Core.MaskedRasterSequence(source, mask, DefaultNoDataValue);
    }

    /// <summary>
    /// Creates a masked sequence that excludes points within the specified bounds.
    /// </summary>
    public IMaskedRasterSequence CreateBoundsExcludedSequence(
        IRasterSequence source,
        RasterBounds bounds)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var mask = new Core.BoundsRasterMask(bounds, includeInterior: false);
        return new Core.MaskedRasterSequence(source, mask, DefaultNoDataValue);
    }

    /// <inheritdoc />
    public IMaskedRasterSequence CreateCustomMaskedSequence(
        IRasterSequence source,
        IRasterMask mask,
        double noDataValue = double.NaN)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));

        return new Core.MaskedRasterSequence(source, mask, noDataValue);
    }
}
