namespace FantaSim.Raster.Contracts.Rendering;

internal sealed class AgeGridPalette : IRasterPalette
{
    public string Name => "AgeGrid";
    public uint NoDataColor => 0xFF000000; // Black with full alpha

    public bool IsContinuous => true;

    public uint GetColor(double value)
    {
        // Map value to 0-1 range (assuming 0-200 Ma for age grids)
        var normalized = Math.Clamp(value / 200.0, 0.0, 1.0);
        return GetColorForNormalized(normalized);
    }

    public uint GetColorForNormalized(double normalized)
    {
        // Blue (young) to Red (old) gradient
        var n = Math.Clamp(normalized, 0.0, 1.0);
        var r = (byte)(n * 255);
        var g = (byte)(0);
        var b = (byte)((1 - n) * 255);
        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }
}
