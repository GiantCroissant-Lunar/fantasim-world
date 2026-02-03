using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using FantaSim.Geosphere.Plate.Runtime.Des.Storage.Documents;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Storage;

/// <summary>
/// Repository for Plate entities using UnifyStorage IDocumentStore.
/// </summary>
public sealed class PlateRepository
{
    private readonly IDocumentStore _documentStore;
    private const string CollectionName = "plates";

    public PlateRepository(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    /// <summary>
    /// Get a plate by its ID.
    /// </summary>
    public async Task<PlateEntity?> GetAsync(PlateId plateId, CancellationToken ct = default)
    {
        var doc = await _documentStore.GetAsync<PlateDocument>(CollectionName, plateId.ToString(), ct);
        return doc?.ToEntity();
    }

    /// <summary>
    /// Upsert a plate.
    /// </summary>
    public Task UpsertAsync(PlateEntity plate, CancellationToken ct = default)
    {
        var doc = PlateDocument.FromEntity(plate);
        return _documentStore.UpsertAsync(CollectionName, plate.PlateId.ToString(), doc, ct);
    }

    /// <summary>
    /// Query plates by retirement status.
    /// </summary>
    public async IAsyncEnumerable<PlateEntity> QueryByRetirementStatusAsync(bool isRetired, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var doc in _documentStore.QueryAsync<PlateDocument>(CollectionName, p => p.IsRetired == isRetired, null, ct))
        {
            yield return doc.ToEntity();
        }
    }

    /// <summary>
    /// Delete a plate by ID.
    /// </summary>
    public Task<bool> DeleteAsync(PlateId plateId, CancellationToken ct = default)
        => _documentStore.DeleteAsync(CollectionName, plateId.ToString(), ct);
}
