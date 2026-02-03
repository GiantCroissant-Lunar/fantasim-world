using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Metadata associated with a reconstruction query.
/// </summary>
[MessagePackObject]
public sealed record QueryMetadata
{
    /// <summary>
    /// Gets the query execution timestamp (UTC).
    /// </summary>
    [Key(0)]
    public required DateTimeOffset ExecutedAt { get; init; }

    /// <summary>
    /// Gets the query execution duration.
    /// </summary>
    [Key(1)]
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the cache hit status.
    /// </summary>
    [Key(2)]
    public bool CacheHit { get; init; }

    /// <summary>
    /// Gets the cache key used (if applicable).
    /// </summary>
    [Key(3)]
    public string? CacheKey { get; init; }

    /// <summary>
    /// Gets any warnings generated during query execution.
    /// </summary>
    [Key(4)]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the solver version identifier.
    /// </summary>
    [Key(5)]
    public required string SolverVersion { get; init; }

    /// <summary>
    /// Creates metadata for a cache hit scenario.
    /// </summary>
    public static QueryMetadata ForCacheHit(string cacheKey, string solverVersion) => new()
    {
        ExecutedAt = DateTimeOffset.UtcNow,
        Duration = TimeSpan.Zero,
        CacheHit = true,
        CacheKey = cacheKey,
        SolverVersion = solverVersion
    };

    /// <summary>
    /// Creates metadata for a computed result.
    /// </summary>
    public static QueryMetadata ForComputed(TimeSpan duration, string solverVersion, IEnumerable<string>? warnings = null) => new()
    {
        ExecutedAt = DateTimeOffset.UtcNow,
        Duration = duration,
        CacheHit = false,
        SolverVersion = solverVersion,
        Warnings = warnings?.ToArray() ?? Array.Empty<string>()
    };
}
