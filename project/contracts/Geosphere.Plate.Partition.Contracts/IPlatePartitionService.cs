namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Service interface for plate partition operations.
/// Provides high-level API for computing plate polygon partitions.
/// RFC-V2-0047 §8.
/// </summary>
public interface IPlatePartitionService
{
    /// <summary>
    /// Computes the plate partition for the given request.
    /// </summary>
    /// <param name="request">The partition request containing tick, tolerance policy, and options.</param>
    /// <returns>The partition result containing plate polygons, metrics, and provenance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the partition cannot be computed due to invalid topology or configuration.
    /// </exception>
    PlatePartitionResult Partition(PartitionRequest request);

    /// <summary>
    /// Asynchronously computes the plate partition for the given request.
    /// </summary>
    /// <param name="request">The partition request containing tick, tolerance policy, and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous partition operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the partition cannot be computed due to invalid topology or configuration.
    /// </exception>
    Task<PlatePartitionResult> PartitionAsync(PartitionRequest request, CancellationToken cancellationToken = default);
}
