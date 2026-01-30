using System;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

/// <summary>
/// Uniquely identifies a stream of truth events in the system.
///
/// A truth stream is the authoritative sequence of events for a specific domain
/// within a specific world variant and branch. It forms the primary key for
/// event storage and retrieval.
/// </summary>
/// <param name="VariantId">The world variant (e.g. "main", "experimental-1").</param>
/// <param name="BranchId">The branch within the variant (e.g. "trunk", "scenario-a").</param>
/// <param name="LLevel">The truth L-level (see RFC-086: L×R×M Axis Model; R is not part of stream identity).</param>
/// <param name="Domain">Stable domain identifier for routing and storage (dot-notation, e.g. "geo.plates.topology").</param>
/// <param name="Model">The model identifier (e.g. "M0").</param>
[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct TruthStreamIdentity(
    [property: UnifyProperty(0)] string VariantId,
    [property: UnifyProperty(1)] string BranchId,
    [property: UnifyProperty(2)] int LLevel,
    [property: UnifyProperty(3)] Domain Domain,
    [property: UnifyProperty(4)] string Model
)
{
    /// <summary>
    /// Returns a string representation of the identity in standard URN format.
    /// Format: urn:fantasim:{VariantId}:{BranchId}:L{LLevel}:{Domain}:{Model}
    /// </summary>
    public override string ToString() =>
        $"urn:fantasim:{VariantId}:{BranchId}:L{LLevel}:{Domain}:{NormalizeModel(Model)}";

    /// <summary>
    /// Parses a URN string into a TruthStreamIdentity.
    /// </summary>
    public static TruthStreamIdentity Parse(string urn)
    {
        if (!TryParse(urn, out var identity))
        {
            throw new FormatException($"Invalid TruthStreamIdentity URN: {urn}");
        }
        return identity;
    }

    public static bool TryParse(string urn, out TruthStreamIdentity identity)
    {
        identity = default;
        if (string.IsNullOrWhiteSpace(urn) || !urn.StartsWith("urn:fantasim:", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = urn.Substring("urn:fantasim:".Length).Split(':');
        if (parts.Length != 5)
        {
            return false;
        }

        if (!parts[2].StartsWith("L", StringComparison.Ordinal) || !int.TryParse(parts[2].Substring(1), out var lLevel))
        {
            return false;
        }

        if (!Domain.TryParse(parts[3], out var domain))
        {
            return false;
        }

        var model = NormalizeModel(parts[4]);
        identity = new TruthStreamIdentity(parts[0], parts[1], lLevel, domain, model);
        return true;
    }

    /// <summary>
    /// Returns the deterministic stream key for this identity.
    /// Format: {VariantId}:{BranchId}:L{LLevel}:{Domain}:{Model}
    /// </summary>
    public string ToStreamKey() =>
        $"{VariantId}:{BranchId}:L{LLevel}:{Domain}:{NormalizeModel(Model)}";

    /// <summary>
    /// Returns the canonical event stream identity string for fingerprinting.
    /// Format: S:{streamKey}:Events
    ///
    /// This format is per RFC-V2-0006 §5.3 for use in InputFingerprint computation.
    /// </summary>
    public string ToEventStreamIdString() =>
        $"S:{ToStreamKey()}:Events";

    private static string NormalizeModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return model;

        var allDigits = true;
        foreach (var c in model)
        {
            if (!char.IsDigit(c))
            {
                allDigits = false;
                break;
            }
        }

        if (allDigits)
            return "M" + model;

        if (model[0] == 'm')
            return "M" + model.Substring(1);

        return model;
    }

    /// <summary>
    /// Validates that the identity components are well-formed.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(VariantId) &&
               !string.IsNullOrWhiteSpace(BranchId) &&
               LLevel >= 0 &&
               !Domain.IsEmpty &&
               !string.IsNullOrWhiteSpace(Model);
    }
}
