namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

public readonly record struct DerivedProductAuditRecord(
    string ProducedAtUtc,
    FantaSim.Geosphere.Plate.Cache.Contracts.DerivedProductKey Key,
    FantaSim.Geosphere.Plate.Cache.Contracts.DerivedProductProvenance Provenance);
