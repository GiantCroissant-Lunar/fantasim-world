using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

public sealed record ArtifactCacheOptions(
    StorageMode Mode,
    bool VerifyOnRead = true,
    string? ColumnFamily = "derived",
    RetentionPolicy? Retention = null);
