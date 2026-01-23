namespace Plate.Topology.Contracts.Capabilities;

/// <summary>
/// Controls how tick-based materialization iterates through events.
///
/// Background: Events are ordered by Sequence number, but their Tick values
/// (simulation time) may not be monotonic. This creates a tradeoff:
/// - Correctness requires scanning all events if ticks might decrease
/// - Performance benefits from breaking early if ticks are guaranteed monotonic
///
/// This enum controls that tradeoff.
/// </summary>
public enum TickMaterializationMode
{
    /// <summary>
    /// Scan all events and apply those with tick &lt;= targetTick.
    /// Correct for all streams regardless of tick monotonicity.
    /// This is the safest option but may be slower for large streams.
    /// </summary>
    ScanAll = 0,

    /// <summary>
    /// Break iteration on first event where tick &gt; targetTick.
    /// ONLY safe if the stream is proven tick-monotone from genesis.
    /// Using this on a non-monotone stream will produce incorrect results.
    /// </summary>
    BreakOnFirstBeyondTick = 1,

    /// <summary>
    /// Default: automatically choose based on stream capabilities.
    /// - If store can prove tick monotonicity → BreakOnFirstBeyondTick
    /// - Otherwise → ScanAll
    ///
    /// This gives optimal performance when safe, and correct results always.
    /// </summary>
    Auto = 2
}
