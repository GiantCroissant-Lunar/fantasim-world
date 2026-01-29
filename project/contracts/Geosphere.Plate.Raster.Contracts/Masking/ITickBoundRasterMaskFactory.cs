using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Raster.Contracts.Masking;

/// <summary>
/// Factory that creates tick-specific raster masks.
/// This is the preferred pattern for time-varying masks (e.g., plate polygons that change over time).
/// RFC-V2-0028 §3.3.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when the mask geometry depends on the tick (e.g., reconstructed plate polygons).
/// The factory encapsulates the logic for obtaining tick-appropriate geometry, keeping callers simple.
/// </para>
/// <para>
/// For static masks (bounds, fixed regions), use <see cref="IRasterMask"/> directly.
/// </para>
/// </remarks>
public interface ITickBoundRasterMaskFactory
{
    /// <summary>
    /// Creates a mask for the specified tick.
    /// </summary>
    /// <param name="tick">The tick for which to create the mask.</param>
    /// <returns>A mask appropriate for the given tick.</returns>
    IRasterMask CreateMask(CanonicalTick tick);
}
