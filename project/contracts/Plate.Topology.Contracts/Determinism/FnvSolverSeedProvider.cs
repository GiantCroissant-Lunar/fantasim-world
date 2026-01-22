using Plate.TimeDete.Determinism.Abstractions;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Determinism;

/// <summary>
/// Default implementation of ISolverSeedProvider using FNV-1a hash for seed derivation.
///
/// Derivation algorithm "FNV1a-StreamIdentity-v2":
/// 1. Start with FNV offset basis
/// 2. Mix in scenario seed
/// 3. Mix in stream identity components in canonical order with length-prefixing:
///    VariantId, BranchId, LLevel, Domain, Model
///    Each string is prefixed with its length to prevent collision between
///    (VariantId="a", BranchId="bc") and (VariantId="ab", BranchId="c").
/// 4. Finalize with avalanche function
///
/// This produces well-distributed seeds even for similar inputs.
/// </summary>
public sealed class FnvSolverSeedProvider : ISolverSeedProvider
{
    private const string AlgorithmName = "FNV1a-StreamIdentity-v2";
    private const ulong FnvOffsetBasis = 0xcbf29ce484222325UL;
    private const ulong FnvPrime = 0x100000001b3UL;

    private readonly ISeededRngFactory _rngFactory;

    /// <summary>
    /// Creates a new FnvSolverSeedProvider with the specified RNG factory.
    /// </summary>
    /// <param name="rngFactory">Factory for creating seeded RNG instances.</param>
    public FnvSolverSeedProvider(ISeededRngFactory rngFactory)
    {
        _rngFactory = rngFactory ?? throw new ArgumentNullException(nameof(rngFactory));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when stream identity is not valid.</exception>
    public ulong DeriveSeed(ulong scenarioSeed, TruthStreamIdentity stream)
    {
        // Validate stream identity to prevent accidental seed sharing
        if (!stream.IsValid())
        {
            throw new ArgumentException(
                $"TruthStreamIdentity is not valid: {stream}. " +
                "Cannot derive seed from invalid identity as it may cause unintended collisions.",
                nameof(stream));
        }

        // Start with FNV offset and mix in scenario seed
        var hash = FnvOffsetBasis;
        hash = MixUInt64(hash, scenarioSeed);

        // Mix in stream identity components in canonical order with length-prefixing
        // Length-prefix prevents collision: ("a","bc") vs ("ab","c") now differ
        hash = MixLengthPrefixedString(hash, stream.VariantId);
        hash = MixLengthPrefixedString(hash, stream.BranchId);
        hash = MixInt32(hash, stream.LLevel);
        hash = MixLengthPrefixedString(hash, stream.Domain.Value);
        hash = MixLengthPrefixedString(hash, stream.Model);

        // Final avalanche to improve distribution
        return Avalanche(hash);
    }

    /// <inheritdoc />
    public ISeededRng CreateRng(ulong scenarioSeed, TruthStreamIdentity stream)
    {
        var derivedSeed = DeriveSeed(scenarioSeed, stream);
        return _rngFactory.Create(derivedSeed);
    }

    /// <inheritdoc />
    public SeedDerivationAuditRecord GetSeedAuditRecord(ulong scenarioSeed, TruthStreamIdentity stream)
    {
        var derivedSeed = DeriveSeed(scenarioSeed, stream);
        return new SeedDerivationAuditRecord(
            ScenarioSeed: scenarioSeed,
            StreamIdentity: stream,
            DerivedSeed: derivedSeed,
            DerivationAlgorithm: AlgorithmName
        );
    }

    private static ulong MixUInt64(ulong hash, ulong value)
    {
        // Mix 8 bytes of the value
        for (int i = 0; i < 8; i++)
        {
            hash ^= (value >> (i * 8)) & 0xFF;
            hash *= FnvPrime;
        }
        return hash;
    }

    private static ulong MixInt32(ulong hash, int value)
    {
        // Mix 4 bytes of the value
        for (int i = 0; i < 4; i++)
        {
            hash ^= (uint)((value >> (i * 8)) & 0xFF);
            hash *= FnvPrime;
        }
        return hash;
    }

    /// <summary>
    /// Mix a string with length-prefix to prevent concatenation collisions.
    /// E.g., ("a","bc") vs ("ab","c") will produce different hashes.
    /// </summary>
    private static ulong MixLengthPrefixedString(ulong hash, string value)
    {
        // First mix the length as a 32-bit value
        hash = MixInt32(hash, value.Length);

        // Then mix the characters
        foreach (var c in value)
        {
            hash ^= c;
            hash *= FnvPrime;
        }
        return hash;
    }

    private static ulong Avalanche(ulong hash)
    {
        // Finalization mix (similar to SplitMix64)
        hash = (hash ^ (hash >> 30)) * 0xbf58476d1ce4e5b9UL;
        hash = (hash ^ (hash >> 27)) * 0x94d049bb133111ebUL;
        return hash ^ (hash >> 31);
    }
}
