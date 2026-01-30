using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Polygonization.Tests.CMap;

/// <summary>
/// Computes a deterministic signature for a CMap structure.
///
/// This helper enables simple equality comparisons in determinism tests:
/// two CMaps are structurally identical iff their signatures match.
///
/// The signature is computed using DeterministicOrder to ensure stability
/// across builds, input orderings, and platforms.
/// </summary>
public static class CMapSignature
{
    /// <summary>
    /// Computes a deterministic signature string for the CMap.
    /// Returns identical values for structurally identical CMaps regardless of build order.
    /// </summary>
    public static string Compute(IBoundaryCMap cmap)
    {
        var sb = new StringBuilder();

        // 1. Ordered junctions
        var junctions = cmap.Junctions.OrderBy(j => j.Value).ToList();
        sb.AppendLine($"JUNCTIONS:{junctions.Count}");
        foreach (var jid in junctions)
        {
            sb.AppendLine($"  J:{jid.Value:N}");

            // Incident darts in cyclic order (this is the key observable contract)
            var incident = cmap.IncidentOrdered(jid);
            sb.AppendLine($"    INCIDENT:{incident.Count}");
            foreach (var dart in incident)
            {
                var twin = cmap.Twin(dart);
                var next = cmap.Next(dart);
                sb.AppendLine($"      D:{FormatDart(dart)}->T:{FormatDart(twin)}->N:{FormatDart(next)}");
            }
        }

        // 2. Ordered darts (all darts with their relations)
        var darts = cmap.Darts.OrderBy(d => d).ToList();
        sb.AppendLine($"DARTS:{darts.Count}");
        foreach (var dart in darts)
        {
            var origin = cmap.Origin(dart);
            var twin = cmap.Twin(dart);
            var next = cmap.Next(dart);
            sb.AppendLine($"  {FormatDart(dart)}:O={origin.Value:N},T={FormatDart(twin)},N={FormatDart(next)}");
        }

        // 3. Faces (normalized: each face rotated to start with smallest dart)
        var faces = cmap.EnumerateFaces()
            .Select(NormalizeFaceLoop)
            .OrderBy(f => f)
            .ToList();
        sb.AppendLine($"FACES:{faces.Count}");
        foreach (var face in faces)
        {
            sb.AppendLine($"  {face}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes a compact hash of the CMap signature for quick equality checks.
    /// </summary>
    public static string ComputeHash(IBoundaryCMap cmap)
    {
        var signature = Compute(cmap);
        var bytes = Encoding.UTF8.GetBytes(signature);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a face loop by rotating it to start with the smallest dart.
    /// This ensures the same face produces the same string regardless of where we started walking.
    /// </summary>
    private static string NormalizeFaceLoop(IReadOnlyList<BoundaryDart> face)
    {
        if (face.Count == 0) return "";

        // Find the smallest dart index
        var minIndex = 0;
        for (int i = 1; i < face.Count; i++)
        {
            if (face[i].CompareTo(face[minIndex]) < 0)
                minIndex = i;
        }

        // Rotate to start with smallest
        var rotated = new List<BoundaryDart>(face.Count);
        for (int i = 0; i < face.Count; i++)
        {
            rotated.Add(face[(minIndex + i) % face.Count]);
        }

        return string.Join("->", rotated.Select(FormatDart));
    }

    /// <summary>
    /// Formats a dart as a compact, deterministic string.
    /// </summary>
    private static string FormatDart(BoundaryDart dart)
    {
        var dir = dart.Direction == DartDirection.Forward ? "F" : "B";
        return $"{dart.BoundaryId.Value:N}[{dart.SegmentIndex}]{dir}";
    }
}

/// <summary>
/// Extension methods for CMap signature comparison in tests.
/// </summary>
public static class CMapSignatureExtensions
{
    /// <summary>
    /// Asserts that two CMaps have identical signatures.
    /// </summary>
    public static void AssertSignaturesEqual(this IBoundaryCMap expected, IBoundaryCMap actual, string? message = null)
    {
        var sigExpected = CMapSignature.Compute(expected);
        var sigActual = CMapSignature.Compute(actual);

        if (!string.Equals(sigExpected, sigActual, StringComparison.Ordinal))
        {
            var msg = message ?? "CMap signatures do not match";
            throw new Xunit.Sdk.XunitException(
                $"{msg}\n\n--- Expected ---\n{sigExpected}\n\n--- Actual ---\n{sigActual}");
        }
    }

    /// <summary>
    /// Asserts that all CMaps in the collection have identical signatures.
    /// </summary>
    public static void AssertAllSignaturesEqual(this IEnumerable<IBoundaryCMap> cmaps, string? message = null)
    {
        var list = cmaps.ToList();
        if (list.Count < 2) return;

        var baselineHash = CMapSignature.ComputeHash(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            var hash = CMapSignature.ComputeHash(list[i]);
            if (!string.Equals(hash, baselineHash, StringComparison.Ordinal))
            {
                var msg = message ?? $"CMap at index {i} has different signature than baseline";
                list[0].AssertSignaturesEqual(list[i], msg);
            }
        }
    }
}
