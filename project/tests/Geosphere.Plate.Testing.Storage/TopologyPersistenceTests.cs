using FantaSim.Geosphere.Plate.Runtime.Des.Storage.Documents;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Testing.Storage;

/// <summary>
/// Tests for round-trip persistence of topology entities using document store.
/// </summary>
public sealed class TopologyPersistenceTests
{
    private readonly IDocumentStore _documentStore;

    public TopologyPersistenceTests(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    /// <summary>
    /// Tests that a PlateDocument can be persisted and retrieved with all data intact.
    /// </summary>
    public async Task<PlateDocument> TestPlateRoundTripAsync(CancellationToken ct = default)
    {
        var originalPlate = new PlateDocument
        {
            PlateId = Guid.NewGuid(),
            IsRetired = false,
            RetirementReason = null
        };

        // Upsert
        await _documentStore.UpsertAsync("plates", originalPlate.PlateId.ToString(), originalPlate, ct)
            .ConfigureAwait(false);

        // Retrieve
        var retrievedPlate = await _documentStore.GetAsync<PlateDocument>(
            "plates", originalPlate.PlateId.ToString(), ct).ConfigureAwait(false);

        // Verify
        if (retrievedPlate is null)
        {
            throw new InvalidOperationException("Failed to retrieve persisted plate.");
        }

        if (retrievedPlate.PlateId != originalPlate.PlateId)
        {
            throw new InvalidOperationException("PlateId mismatch after round-trip.");
        }

        if (retrievedPlate.IsRetired != originalPlate.IsRetired)
        {
            throw new InvalidOperationException("IsRetired mismatch after round-trip.");
        }

        return retrievedPlate;
    }

    /// <summary>
    /// Tests that a JunctionDocument can be persisted and retrieved with all data intact.
    /// </summary>
    public async Task<JunctionDocument> TestJunctionRoundTripAsync(CancellationToken ct = default)
    {
        var originalJunction = new JunctionDocument
        {
            JunctionId = Guid.NewGuid(),
            BoundaryIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            NormalX = 0,
            NormalY = 0,
            NormalZ = 1,
            Radius = 6371000, // Earth radius in meters
            IsRetired = false,
            RetirementReason = null
        };

        // Upsert
        await _documentStore.UpsertAsync("junctions", originalJunction.JunctionId.ToString(), originalJunction, ct)
            .ConfigureAwait(false);

        // Retrieve
        var retrievedJunction = await _documentStore.GetAsync<JunctionDocument>(
            "junctions", originalJunction.JunctionId.ToString(), ct).ConfigureAwait(false);

        // Verify
        if (retrievedJunction is null)
        {
            throw new InvalidOperationException("Failed to retrieve persisted junction.");
        }

        if (retrievedJunction.JunctionId != originalJunction.JunctionId)
        {
            throw new InvalidOperationException("JunctionId mismatch after round-trip.");
        }

        if (retrievedJunction.NormalX != originalJunction.NormalX)
        {
            throw new InvalidOperationException("NormalX mismatch after round-trip.");
        }

        return retrievedJunction;
    }

    /// <summary>
    /// Tests batch operations for multiple plates.
    /// </summary>
    public async Task TestBatchPlateOperationsAsync(CancellationToken ct = default)
    {
        var plates = new List<PlateDocument>
        {
            new() { PlateId = Guid.NewGuid(), IsRetired = false, RetirementReason = null },
            new() { PlateId = Guid.NewGuid(), IsRetired = false, RetirementReason = null },
            new() { PlateId = Guid.NewGuid(), IsRetired = true, RetirementReason = "Test retirement" }
        };

        // Upsert all
        foreach (var plate in plates)
        {
            await _documentStore.UpsertAsync("plates", plate.PlateId.ToString(), plate, ct)
                .ConfigureAwait(false);
        }

        // Query retired plates
        var retiredPlates = new List<PlateDocument>();
        await foreach (var plate in _documentStore.QueryAsync<PlateDocument>(
            "plates", p => p.IsRetired, null, ct).ConfigureAwait(false))
        {
            retiredPlates.Add(plate);
        }

        if (retiredPlates.Count != 1)
        {
            throw new InvalidOperationException($"Expected 1 retired plate, found {retiredPlates.Count}.");
        }

        // Delete all
        foreach (var plate in plates)
        {
            var deleted = await _documentStore.DeleteAsync("plates", plate.PlateId.ToString(), ct)
                .ConfigureAwait(false);
            if (!deleted)
            {
                throw new InvalidOperationException($"Failed to delete plate {plate.PlateId}.");
            }
        }
    }
}
