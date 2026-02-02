namespace FantaSim.Raster.Contracts.Rendering;

internal sealed class ElevationPalette : IRasterPalette
{
    public string Name => "Elevation";
    public uint NoDataColor => 0xFF00FF00; // Green with full alpha

    public bool IsContinuous => true;

    public uint GetColor(double value)
    {
        // Map value to 0-1 range (assuming -11000 to 8848 meters)
        var normalized = Math.Clamp((value + 11000) / 19848.0, 0.0, 1.0);
        return GetColorForNormalized(normalized);
    }

    public uint GetColorForNormalized(double normalized)
    {
        var n = Math.Clamp(normalized, 0.0, 1.0);
        byte r, g, b;

        if (n < 0.5)
        {
            // Blue to white (ocean)
            var t = n * 2;
            r = g = b = (byte)(t * 255);
        }
        else
        {
            // White to brown (land)
            var t = (n - 0.5) * 2;
            r = (byte)(139 + (255 - 139) * (1 - t));
            g = (byte)(69 + (255 - 69) * (1 - t));
            b = (byte)(19 + (255 - 19) * (1 - t));
        }

        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }
}
