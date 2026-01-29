using FantaSim.Geosphere.Plate.Raster.Contracts;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Raster.Masking;

/// <summary>
/// Factory for creating masked raster sequences.
/// RFC-V2-0028 compliant.
/// </summary>
public sealed class RasterMaskFactory : IRasterMaskFactory
{
    private const double DefaultNoDataValue = double.NaN;

    /// <inheritdoc />
    public IMaskedRasterSequence CreatePlateMaskedSequence(
        IRasterSequence source,
        PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var mask = new PlatePolygonRasterMask(polygonSet, specificPlates);
        return new MaskedRasterSequence(source, mask, DefaultNoDataValue, polygonSet);
    }

    /// <inheritdoc />
    public IMaskedRasterSequence CreateBoundsMaskedSequence(
        IRasterSequence source,
        RasterBounds bounds)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var mask = new BoundsRasterMask(bounds, includeInterior: true);
        return new MaskedRasterSequence(source, mask, DefaultNoDataValue);
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

        var mask = new BoundsRasterMask(bounds, includeInterior: false);
        return new MaskedRasterSequence(source, mask, DefaultNoDataValue);
    }

    /// <summary>
    /// Creates a masked sequence using a custom mask.
    /// </summary>
    public IMaskedRasterSequence CreateCustomMaskedSequence(
        IRasterSequence source,
        IRasterMask mask,
        double noDataValue = double.NaN)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));

        return new MaskedRasterSequence(source, mask, noDataValue);
    }
}
