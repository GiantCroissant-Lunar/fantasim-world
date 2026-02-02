namespace FantaSim.Raster.Contracts.Masking;

/// <summary>
/// Current schema version for mask specifications.
/// Increment when the serialization format changes in a breaking way.
/// </summary>
public static class MaskSpecVersions
{
    /// <summary>
    /// Version 1: Initial schema with Bounds and IncludeInterior.
    /// </summary>
    public const int BoundsMaskSpecV1 = 1;
}
