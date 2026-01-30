using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Raster.Contracts.Export;

/// <summary>
/// Specification for exporting a raster sequence over a tick range.
/// RFC-V2-0028 §4.
/// </summary>
[MessagePackObject]
public readonly record struct RasterExportSpec(
    [property: Key(0)] string SequenceId,
    [property: Key(1)] CanonicalTick StartTick,
    [property: Key(2)] CanonicalTick EndTick,
    [property: Key(3)] int TickStep,
    [property: Key(4)] RasterExportFormat Format,
    [property: Key(5)] RasterQueryOptions QueryOptions,
    [property: Key(6)] string OutputDirectory,
    [property: Key(7)] string FileNameTemplate
)
{
    /// <summary>
    /// Validates the export specification.
    /// </summary>
    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(SequenceId))
        {
            error = "SequenceId is required";
            return false;
        }

        if (EndTick < StartTick)
        {
            error = "EndTick must be >= StartTick";
            return false;
        }

        if (TickStep <= 0)
        {
            error = "TickStep must be > 0";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            error = "OutputDirectory is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FileNameTemplate))
        {
            error = "FileNameTemplate is required";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Gets the ticks to export based on StartTick, EndTick, and TickStep.
    /// </summary>
    public IEnumerable<CanonicalTick> GetExportTicks()
    {
        for (var tick = StartTick.Value; tick <= EndTick.Value; tick += TickStep)
        {
            yield return new CanonicalTick(tick);
        }
    }

    /// <summary>
    /// Generates the output filename for a specific tick.
    /// Template supports: {tick}, {sequenceId}, {format}
    /// </summary>
    public string GetOutputFileName(CanonicalTick tick)
    {
        var fileName = FileNameTemplate
            .Replace("{tick}", tick.Value.ToString())
            .Replace("{sequenceId}", SequenceId)
            .Replace("{format}", Format.ToString().ToLowerInvariant());

        return fileName;
    }
}
