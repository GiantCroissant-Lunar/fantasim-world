namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed record RetentionPolicy(
    long? MaxSequenceAge = null,
    TimeSpan? MaxTimeAge = null,
    int? MinArtifactsToKeep = null);
