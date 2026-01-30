using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Raster.Contracts;
using FantaSim.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

// Use Core types
using CoreMaskedRasterSequence = FantaSim.Raster.Core.MaskedRasterSequence;

namespace FantaSim.Raster.Masking.Plate;

/// <summary>
/// Backward compatibility alias for <see cref="Core.MaskedRasterSequence"/>.
/// </summary>
/// <remarks>
/// This type has been moved to <c>Raster.Core</c>.
/// For new code, use <c>FantaSim.Raster.Core.MaskedRasterSequence</c>.
/// </remarks>
[Obsolete("Use FantaSim.Raster.Core.MaskedRasterSequence instead.")]
public class MaskedRasterSequence : CoreMaskedRasterSequence
{
    public MaskedRasterSequence(
        IRasterSequence sourceSequence,
        IRasterMask mask,
        double noDataValue)
        : base(sourceSequence, mask, noDataValue)
    {
    }
}

/// <summary>
/// A masked raster sequence that includes plate polygon information.
/// RFC-V2-0028 §8.2.2 - Composition layer implementation.
/// </summary>
/// <remarks>
/// This class extends <see cref="CoreMaskedRasterSequence"/> with plate-specific
/// properties, keeping the base class domain-agnostic.
/// </remarks>
public sealed class PlateMaskedRasterSequence : CoreMaskedRasterSequence, IPlateMaskedRasterSequence
{
    private readonly PlatePolygonSet _polygonSet;
    private readonly IReadOnlyCollection<PlateId>? _specificPlates;

    public PlateMaskedRasterSequence(
        IRasterSequence sourceSequence,
        IRasterMask mask,
        double noDataValue,
        PlatePolygonSet polygonSet,
        IReadOnlyCollection<PlateId>? specificPlates = null)
        : base(sourceSequence, mask, noDataValue)
    {
        _polygonSet = polygonSet;
        _specificPlates = specificPlates;
    }

    /// <inheritdoc />
    public PlatePolygonSet PolygonSet => _polygonSet;

    /// <inheritdoc />
    public IReadOnlyCollection<PlateId>? SpecificPlates => _specificPlates;
}
