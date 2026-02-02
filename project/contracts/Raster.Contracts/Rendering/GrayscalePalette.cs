namespace FantaSim.Raster.Contracts.Rendering;

internal sealed class GrayscalePalette : IRasterPalette
{
    public string Name => "Grayscale";
    public uint NoDataColor => 0xFF000000; // Black with full alpha

    public bool IsContinuous => true;

    public uint GetColor(double value)
    {
        var normalized = Math.Clamp(value, 0.0, 1.0);
        return GetColorForNormalized(normalized);
    }

    public uint GetColorForNormalized(double normalized)
    {
        var n = Math.Clamp(normalized, 0.0, 1.0);
        var gray = (byte)(n * 255);
        return (uint)((255 << 24) | (gray << 16) | (gray << 8) | gray);
    }
}
