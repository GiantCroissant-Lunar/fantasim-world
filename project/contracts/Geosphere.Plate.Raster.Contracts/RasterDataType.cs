namespace FantaSim.Geosphere.Plate.Raster.Contracts;

/// <summary>
/// Data type of raster cell values.
/// RFC-V2-0028 §3.1.
/// </summary>
public enum RasterDataType
{
    /// <summary>8-bit unsigned integer.</summary>
    UInt8,
    
    /// <summary>16-bit unsigned integer.</summary>
    UInt16,
    
    /// <summary>32-bit unsigned integer.</summary>
    UInt32,
    
    /// <summary>8-bit signed integer.</summary>
    Int8,
    
    /// <summary>16-bit signed integer.</summary>
    Int16,
    
    /// <summary>32-bit signed integer.</summary>
    Int32,
    
    /// <summary>32-bit IEEE float.</summary>
    Float32,
    
    /// <summary>64-bit IEEE double.</summary>
    Float64
}
