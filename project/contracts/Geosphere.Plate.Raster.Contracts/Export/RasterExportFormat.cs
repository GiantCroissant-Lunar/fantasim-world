namespace FantaSim.Geosphere.Plate.Raster.Contracts.Export;

/// <summary>
/// Output format for raster sequence exports.
/// RFC-V2-0028 §4.
/// </summary>
public enum RasterExportFormat
{
    /// <summary>GeoTIFF format (.tif)</summary>
    GeoTiff,
    
    /// <summary>PNG image with world file (.png + .pgw)</summary>
    PngWithWorldFile,
    
    /// <summary>NetCDF format (.nc)</summary>
    NetCDF,
    
    /// <summary>Raw binary with header (.bin + .hdr)</summary>
    RawBinary,
    
    /// <summary>CSV with lat/lon/value columns</summary>
    Csv
}
