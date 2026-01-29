using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

/// <summary>
/// Context passed to artifact generators.
/// </summary>
public sealed record ArtifactGenerationContext(
    TruthStreamIdentity Stream,
    long LastSequence,
    string InputFingerprint);
