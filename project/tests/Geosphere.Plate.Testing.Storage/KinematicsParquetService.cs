using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Export;
using UnifyStorage.Abstractions;
using UnifyStorage.Runtime.Parquet;

namespace FantaSim.Geosphere.Plate.Testing.Storage;

/// <summary>
/// Service for exporting and importing kinematics datasets to/from Parquet format.
/// </summary>
public sealed class KinematicsParquetService
{
    private readonly IParquetExporter _exporter;
    private readonly IParquetImporter _importer;
    private readonly IDocumentStore? _documentStore;

    public KinematicsParquetService(
        IParquetExporter exporter,
        IParquetImporter importer,
        IDocumentStore? documentStore = null)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _documentStore = documentStore;
    }

    /// <summary>
    /// Exports motion segment events to Parquet file.
    /// </summary>
    public async Task ExportMotionSegmentsAsync(
        IAsyncEnumerable<MotionSegmentUpsertedEvent> events,
        string outputPath,
        CancellationToken ct = default)
    {
        var records = ToExportRecords(events);
        await _exporter.ExportAsync(records, outputPath, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports motion segments from document store to Parquet file.
    /// </summary>
    public Task ExportMotionSegmentsAsync(
        string outputPath,
        CancellationToken ct = default)
    {
        if (_documentStore is null)
        {
            throw new InvalidOperationException("Document store is required for this operation.");
        }

        return _exporter.ExportAsync<MotionSegmentExportRecord>(
            _documentStore, "motionSegments", outputPath, ct);
    }

    /// <summary>
    /// Imports motion segment records from Parquet file.
    /// </summary>
    public IAsyncEnumerable<MotionSegmentExportRecord> ImportMotionSegmentsAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        return _importer.ReadAsync<MotionSegmentExportRecord>(inputPath, ct);
    }

    private static async IAsyncEnumerable<MotionSegmentExportRecord> ToExportRecords(
        IAsyncEnumerable<MotionSegmentUpsertedEvent> events,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in events.WithCancellation(ct).ConfigureAwait(false))
        {
            var rotation = evt.StageRotation;
            yield return new MotionSegmentExportRecord
            {
                EventId = evt.EventId,
                PlateId = evt.PlateId.ToString(),
                SegmentId = evt.SegmentId.Value,
                TickA = evt.TickA.Value,
                TickB = evt.TickB.Value,
                RotationAngleDegrees = rotation.AngleDeg,
                PoleLatitude = rotation.AxisElevationDeg,
                PoleLongitude = rotation.AxisAzimuthDeg,
                Tick = evt.Tick.Value,
                Sequence = evt.Sequence,
                StreamIdentity = evt.StreamIdentity.ToString(),
                PreviousHash = evt.PreviousHash.ToArray(),
                Hash = evt.Hash.ToArray()
            };
        }
    }
}
