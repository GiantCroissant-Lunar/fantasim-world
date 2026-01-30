using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Raster.Contracts.Masking;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Raster.Masking.Plate;

/// <summary>
/// Factory that creates plate polygon-based masks for a specific tick.
/// Encapsulates the polygonizer + topology lookup so callers don't need to manage tick-to-polygon mapping.
/// RFC-V2-0028 §3.3.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides a single source of truth for "how to get plate polygons at tick T".
/// Callers simply request <c>CreateMask(tick)</c> and receive a ready-to-use mask.
/// </para>
/// <para>
/// For scenarios where the caller already has a <see cref="PlatePolygonSet"/>,
/// use <see cref="PlatePolygonRasterMask"/> directly.
/// </para>
/// </remarks>
public sealed class PlatePolygonRasterMaskFactory : ITickBoundRasterMaskFactory
{
    private readonly IPlatePolygonizer _polygonizer;
    private readonly IPlateTopologyStateView _topology;
    private readonly IReadOnlyCollection<PlateId>? _specificPlates;
    private readonly PolygonizationOptions? _polygonizationOptions;

    /// <summary>
    /// Creates a new PlatePolygonRasterMaskFactory.
    /// </summary>
    /// <param name="polygonizer">The polygonizer for extracting plate polygons.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="specificPlates">Optional: limit masking to specific plates. If null, all plates are included.</param>
    /// <param name="polygonizationOptions">Optional: polygonization options.</param>
    public PlatePolygonRasterMaskFactory(
        IPlatePolygonizer polygonizer,
        IPlateTopologyStateView topology,
        IReadOnlyCollection<PlateId>? specificPlates = null,
        PolygonizationOptions? polygonizationOptions = null)
    {
        _polygonizer = polygonizer ?? throw new ArgumentNullException(nameof(polygonizer));
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        _specificPlates = specificPlates;
        _polygonizationOptions = polygonizationOptions;
    }

    /// <summary>
    /// The polygonizer used to extract plate polygons.
    /// </summary>
    public IPlatePolygonizer Polygonizer => _polygonizer;

    /// <summary>
    /// The topology state view.
    /// </summary>
    public IPlateTopologyStateView Topology => _topology;

    /// <summary>
    /// The specific plates to include (null = all plates).
    /// </summary>
    public IReadOnlyCollection<PlateId>? SpecificPlates => _specificPlates;

    /// <inheritdoc />
    public IRasterMask CreateMask(CanonicalTick tick)
    {
        var polygonSet = _polygonizer.PolygonizeAtTick(tick, _topology, _polygonizationOptions);
        return new PlatePolygonRasterMask(polygonSet, _specificPlates);
    }
}
