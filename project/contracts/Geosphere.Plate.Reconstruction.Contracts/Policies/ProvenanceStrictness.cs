namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

public enum ProvenanceStrictness
{
    Strict,      // Require complete provenance
    Lenient,     // Allow partial with warnings
    Permissive   // Best-effort, continue on missing
}
