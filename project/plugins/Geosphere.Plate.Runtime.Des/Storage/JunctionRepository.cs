using FantaSim.Geosphere.Plate.Runtime.Des.Storage.Documents;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Storage;

/// <summary>
/// Repository for Junction entities using UnifyStorage IDocumentStore.
/// </summary>
public sealed class JunctionRepository
{
    private readonly IDocumentStore _documentStore;
    private const string CollectionName = "junctions";

    public JunctionRepository(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    /// <summary>
    /// Get a junction by its ID.
    /// </summary>
    public async Task<Junction?> GetAsync(JunctionId junctionId, CancellationToken ct = default)
    {
        var doc = await _documentStore.GetAsync<JunctionDocument>(CollectionName, junctionId.ToString(), ct);
        return doc?.ToEntity();
    }

    /// <summary>
    /// Upsert a junction.
    /// </summary>
    public Task UpsertAsync(Junction junction, CancellationToken ct = default)
    {
        var doc = JunctionDocument.FromEntity(junction);
        return _documentStore.UpsertAsync(CollectionName, junction.JunctionId.ToString(), doc, ct);
    }

    /// <summary>
    /// Query junctions by retirement status.
    /// </summary>
    public async IAsyncEnumerable<Junction> QueryByRetirementStatusAsync(bool isRetired, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var doc in _documentStore.QueryAsync<JunctionDocument>(CollectionName, j => j.IsRetired == isRetired, null, ct))
        {
            yield return doc.ToEntity();
        }
    }

    /// <summary>
    /// Delete a junction by ID.
    /// </summary>
    public Task<bool> DeleteAsync(JunctionId junctionId, CancellationToken ct = default)
        => _documentStore.DeleteAsync(CollectionName, junctionId.ToString(), ct);
}
