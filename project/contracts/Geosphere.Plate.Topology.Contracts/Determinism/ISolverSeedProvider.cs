using Plate.TimeDete.Determinism.Abstractions;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Determinism;

/// <summary>
/// Provides deterministic seed derivation from stream identity for solver implementations.
///
/// Per RFC-099 guidance, solvers require:
/// 1. Deterministic seed derivation tied to stream identity
/// 2. Auditable seed provenance
/// 3. Reproducible RNG streams for identical inputs
///
/// Implementations derive seeds from TruthStreamIdentity components, ensuring that:
/// - Same stream identity + scenario seed ??same RNG sequence
/// - Different streams ??independent RNG sequences
/// - Seed derivation is auditable via GetSeedAuditRecord()
/// </summary>
public interface ISolverSeedProvider
{
    /// <summary>
    /// Derives a solver seed from a scenario seed and truth stream identity.
    ///
    /// The derivation is deterministic: identical inputs always produce identical seeds.
    /// This enables replay determinism when the scenario seed is preserved.
    /// </summary>
    /// <param name="scenarioSeed">Base seed for the scenario (e.g., from world config).</param>
    /// <param name="stream">The truth stream identity to incorporate into seed derivation.</param>
    /// <returns>A derived seed unique to this scenario + stream combination.</returns>
    ulong DeriveSeed(ulong scenarioSeed, TruthStreamIdentity stream);

    /// <summary>
    /// Creates an RNG instance seeded for the specified stream.
    ///
    /// This is a convenience method combining DeriveSeed + factory creation.
    /// </summary>
    /// <param name="scenarioSeed">Base seed for the scenario.</param>
    /// <param name="stream">The truth stream identity.</param>
    /// <returns>A seeded RNG instance for deterministic generation.</returns>
    ISeededRng CreateRng(ulong scenarioSeed, TruthStreamIdentity stream);

    /// <summary>
    /// Gets an audit record for a seed derivation operation.
    ///
    /// The audit record contains all inputs and the derived output for traceability.
    /// </summary>
    /// <param name="scenarioSeed">Base seed for the scenario.</param>
    /// <param name="stream">The truth stream identity.</param>
    /// <returns>An audit record capturing the derivation.</returns>
    SeedDerivationAuditRecord GetSeedAuditRecord(ulong scenarioSeed, TruthStreamIdentity stream);
}

/// <summary>
/// Audit record for seed derivation operations per RFC-099 traceability requirements.
/// </summary>
/// <param name="ScenarioSeed">The input scenario seed.</param>
/// <param name="StreamIdentity">The truth stream identity used in derivation.</param>
/// <param name="DerivedSeed">The output derived seed.</param>
/// <param name="DerivationAlgorithm">Identifier for the derivation algorithm (for versioning).</param>
public readonly record struct SeedDerivationAuditRecord(
    ulong ScenarioSeed,
    TruthStreamIdentity StreamIdentity,
    ulong DerivedSeed,
    string DerivationAlgorithm
);
