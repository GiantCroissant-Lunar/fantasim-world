namespace FantaSim.Raster.Contracts.Rendering;

internal sealed class ViridisPalette : IRasterPalette
{
    public string Name => "Viridis";
    public uint NoDataColor => 0xFF000000; // Black with full alpha

    public bool IsContinuous => true;

    public uint GetColor(double value)
    {
        var normalized = Math.Clamp(value, 0.0, 1.0);
        return GetColorForNormalized(normalized);
    }

    public uint GetColorForNormalized(double normalized)
    {
        // Simplified viridis approximation
        var n = Math.Clamp(normalized, 0.0, 1.0);
        byte r, g, b;

        if (n < 0.25)
        {
            var t = n * 4;
            r = (byte)(68 + (33 - 68) * t);
            g = (byte)(1 + (144 - 1) * t);
            b = (byte)(84 + (140 - 84) * t);
        }
        else if (n < 0.5)
        {
            var t = (n - 0.25) * 4;
            r = (byte)(33 + (59 - 33) * t);
            g = (byte)(144 + (82 - 144) * t);
            b = (byte)(140 + (81 - 140) * t);
        }
        else if (n < 0.75)
        {
            var t = (n - 0.5) * 4;
            r = (byte)(59 + (253 - 59) * t);
            g = (byte)(82 + (231 - 82) * t);
            b = (byte)(81 + (37 - 81) * t);
        }
        else
        {
            var t = (n - 0.75) * 4;
            r = (byte)(253 + (253 - 253) * t);
            g = (byte)(231 + (191 - 231) * t);
            b = (byte)(37 + (111 - 37) * t);
        }

        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }
}
