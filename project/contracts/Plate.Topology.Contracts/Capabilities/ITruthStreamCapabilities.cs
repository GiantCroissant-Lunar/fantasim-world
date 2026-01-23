using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Capabilities;

/// <summary>
/// Provides queryable capabilities for a truth stream.
///
/// Capabilities are persisted metadata that describe properties of the stream,
/// such as whether tick monotonicity has been enforced since genesis.
/// These capabilities enable safe optimizations in materialization.
///
/// Implementation note: Capabilities should only be set when provably true.
/// If uncertain, the capability should not be set (query returns false).
/// </summary>
public interface ITruthStreamCapabilities
{
    /// <summary>
    /// Checks if the stream has proven tick monotonicity from genesis.
    ///
    /// Returns true only if:
    /// - The stream was created with TickMonotonicityPolicy.Reject, AND
    /// - That policy has been enforced since the first event (genesis)
    ///
    /// If true, tick-based materialization can safely break early on
    /// first event.Tick &gt; targetTick, improving performance.
    ///
    /// If false or uncertain, callers must scan all events for correctness.
    /// </summary>
    /// <param name="stream">The truth stream identity to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if tick monotonicity is proven from genesis; false otherwise.</returns>
    ValueTask<bool> IsTickMonotoneFromGenesisAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken = default);
}
