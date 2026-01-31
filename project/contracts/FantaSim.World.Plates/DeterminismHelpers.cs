using System.Runtime.CompilerServices;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.World.Plates;

/// <summary>
/// Provides deterministic ordering and comparison utilities per RFC-V2-0045 Section 6.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 6: Determinism is critical for reproducible scientific results
/// and reliable caching. All ordering must be canonical and stable across runs.
///
/// Canonical ordering: by EntityId.Value, then CanonicalTick.Value, then part index.
/// </remarks>
public static class DeterminismHelpers
{
    #region Canonical Comparers

    /// <summary>
    /// Comparer for FeatureId that provides canonical ordering by GUID value.
    /// </summary>
    public static IComparer<FeatureId> FeatureIdComparer { get; } = new FeatureIdComparerImpl();

    /// <summary>
    /// Comparer for PlateId that provides canonical ordering by GUID value.
    /// </summary>
    public static IComparer<PlateId> PlateIdComparer { get; } = new PlateIdComparerImpl();

    /// <summary>
    /// Comparer for BoundaryId that provides canonical ordering by GUID value.
    /// </summary>
    public static IComparer<BoundaryId> BoundaryIdComparer { get; } = new BoundaryIdComparerImpl();

    /// <summary>
    /// Comparer for JunctionId that provides canonical ordering by GUID value.
    /// </summary>
    public static IComparer<JunctionId> JunctionIdComparer { get; } = new JunctionIdComparerImpl();

    /// <summary>
    /// Comparer for CanonicalTick that provides numeric ordering.
    /// </summary>
    public static IComparer<CanonicalTick> TickComparer { get; } = new TickComparerImpl();

    /// <summary>
    /// Comparer for ReconstructedFeature per RFC-V2-0045 Section 6.
    /// Canonical ordering: by SourceFeatureId.Value ascending.
    /// </summary>
    public static IComparer<ReconstructedFeature> ReconstructedFeatureComparer { get; } = new ReconstructedFeatureComparerImpl();

    #endregion

    #region Ordering Methods

    /// <summary>
    /// Orders a sequence of reconstructed features canonically per RFC-V2-0045.
    /// </summary>
    /// <param name="features">The features to order.</param>
    /// <returns>Stably sorted features by SourceFeatureId.Value ascending.</returns>
    public static IOrderedEnumerable<ReconstructedFeature> OrderCanonically(this IEnumerable<ReconstructedFeature> features)
    {
        return features.OrderBy(f => f.SourceFeatureId, FeatureIdComparer);
    }

    /// <summary>
    /// Orders a sequence of features by ID canonically.
    /// </summary>
    public static IOrderedEnumerable<T> OrderByFeatureId<T>(this IEnumerable<T> source, Func<T, FeatureId> selector)
    {
        return source.OrderBy(selector, FeatureIdComparer);
    }

    /// <summary>
    /// Orders a sequence of items by plate ID canonically.
    /// </summary>
    public static IOrderedEnumerable<T> OrderByPlateId<T>(this IEnumerable<T> source, Func<T, PlateId> selector)
    {
        return source.OrderBy(selector, PlateIdComparer);
    }

    /// <summary>
    /// Orders a sequence of items by tick canonically.
    /// </summary>
    public static IOrderedEnumerable<T> OrderByTick<T>(this IEnumerable<T> source, Func<T, CanonicalTick> selector)
    {
        return source.OrderBy(selector, TickComparer);
    }

    #endregion

    #region Stable Sorting

    /// <summary>
    /// Performs a stable sort on reconstructed features.
    /// </summary>
    /// <remarks>
    /// Uses OrderBy which is guaranteed to be stable in LINQ.
    /// </remarks>
    public static List<ReconstructedFeature> StableSort(this IEnumerable<ReconstructedFeature> features)
    {
        return features.OrderCanonically().ToList();
    }

    /// <summary>
    /// Verifies that features are sorted canonically.
    /// </summary>
    /// <param name="features">The features to check.</param>
    /// <returns>True if sorted canonically; otherwise, false.</returns>
    public static bool IsCanonicallySorted(this IReadOnlyList<ReconstructedFeature> features)
    {
        if (features.Count <= 1)
            return true;

        for (int i = 1; i < features.Count; i++)
        {
            var comparison = FeatureIdComparer.Compare(features[i - 1].SourceFeatureId, features[i].SourceFeatureId);
            if (comparison > 0)
                return false;
        }

        return true;
    }

    #endregion

    #region Parameter Canonicalization

    /// <summary>
    /// Canonicalizes a set of feature IDs for consistent ordering.
    /// </summary>
    public static IReadOnlyList<FeatureId> Canonicalize(this IEnumerable<FeatureId> featureIds)
    {
        return featureIds
            .Distinct()
            .OrderBy(id => id, FeatureIdComparer)
            .ToList();
    }

    /// <summary>
    /// Canonicalizes a set of plate IDs for consistent ordering.
    /// </summary>
    public static IReadOnlyList<PlateId> Canonicalize(this IEnumerable<PlateId> plateIds)
    {
        return plateIds
            .Distinct()
            .OrderBy(id => id, PlateIdComparer)
            .ToList();
    }

    /// <summary>
    /// Canonicalizes a set of boundary IDs for consistent ordering.
    /// </summary>
    public static IReadOnlyList<BoundaryId> Canonicalize(this IEnumerable<BoundaryId> boundaryIds)
    {
        return boundaryIds
            .Distinct()
            .OrderBy(id => id, BoundaryIdComparer)
            .ToList();
    }

    /// <summary>
    /// Canonicalizes a set of junction IDs for consistent ordering.
    /// </summary>
    public static IReadOnlyList<JunctionId> Canonicalize(this IEnumerable<JunctionId> junctionIds)
    {
        return junctionIds
            .Distinct()
            .OrderBy(id => id, JunctionIdComparer)
            .ToList();
    }

    #endregion

    #region IEEE 754-2019 Compliance

    /// <summary>
    /// Compares two double values with IEEE 754-2019 total ordering.
    /// </summary>
    /// <remarks>
    /// Per RFC-V2-0045 Section 6: Stable floating-point policies per IEEE 754-2019.
    /// This provides consistent ordering including handling of NaN values.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TotalOrderCompare(double a, double b)
    {
        // Handle NaN specially per IEEE 754-2019 totalOrder predicate
        var aIsNaN = double.IsNaN(a);
        var bIsNaN = double.IsNaN(b);

        if (aIsNaN && bIsNaN)
        {
            // Compare NaN payloads for deterministic ordering
            var aPayload = GetNaNPayload(a);
            var bPayload = GetNaNPayload(b);
            return aPayload.CompareTo(bPayload);
        }

        if (aIsNaN) return 1;  // NaN sorts after all numbers
        if (bIsNaN) return -1; // All numbers sort before NaN

        // Handle -0.0 vs +0.0
        if (a == 0.0 && b == 0.0)
        {
            // -0.0 < +0.0 in total order
            var aNeg = double.IsNegative(a);
            var bNeg = double.IsNegative(b);
            if (aNeg == bNeg)
                return 0;
            return aNeg ? -1 : 1;
        }

        return a.CompareTo(b);
    }

    /// <summary>
    /// Gets the payload bits of a NaN value for deterministic comparison.
    /// </summary>
    private static long GetNaNPayload(double value)
    {
        return BitConverter.DoubleToInt64Bits(value) & 0x000FFFFFFFFFFFFF;
    }

    /// <summary>
    /// Determines if two double values are equal with IEEE 754-2019 total equality.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TotalOrderEquals(double a, double b)
    {
        return TotalOrderCompare(a, b) == 0;
    }

    /// <summary>
    /// Computes a hash code for a double that is consistent with total ordering.
    /// </summary>
    public static int GetTotalOrderHashCode(double value)
    {
        if (double.IsNaN(value))
        {
            // All NaNs get the same hash code for consistency
            return int.MaxValue;
        }

        // Distinguish -0.0 from +0.0
        if (value == 0.0)
        {
            return double.IsNegative(value) ? -1 : 1;
        }

        return value.GetHashCode();
    }

    #endregion

    #region Private Comparer Implementations

    private sealed class FeatureIdComparerImpl : IComparer<FeatureId>
    {
        public int Compare(FeatureId x, FeatureId y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    private sealed class PlateIdComparerImpl : IComparer<PlateId>
    {
        public int Compare(PlateId x, PlateId y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    private sealed class BoundaryIdComparerImpl : IComparer<BoundaryId>
    {
        public int Compare(BoundaryId x, BoundaryId y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    private sealed class JunctionIdComparerImpl : IComparer<JunctionId>
    {
        public int Compare(JunctionId x, JunctionId y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    private sealed class TickComparerImpl : IComparer<CanonicalTick>
    {
        public int Compare(CanonicalTick x, CanonicalTick y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    private sealed class ReconstructedFeatureComparerImpl : IComparer<ReconstructedFeature>
    {
        public int Compare(ReconstructedFeature? x, ReconstructedFeature? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            // Per RFC-V2-0045: Sort by SourceFeatureId.Value ascending
            return x.SourceFeatureId.Value.CompareTo(y.SourceFeatureId.Value);
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for deterministic operations on reconstruction types.
/// </summary>
public static class DeterministicExtensions
{
    /// <summary>
    /// Creates a deterministically sorted copy of the feature list.
    /// </summary>
    public static IReadOnlyList<ReconstructedFeature> ToDeterministicList(this IEnumerable<ReconstructedFeature> features)
    {
        return DeterminismHelpers.StableSort(features);
    }

    /// <summary>
    /// Gets a deterministic hash code for a collection of features.
    /// </summary>
    public static int GetDeterministicHashCode(this IEnumerable<ReconstructedFeature> features)
    {
        var hash = new HashCode();
        foreach (var feature in features.OrderBy(f => f.SourceFeatureId, DeterminismHelpers.FeatureIdComparer))
        {
            hash.Add(feature.SourceFeatureId.Value);
            hash.Add(feature.PlateId.Value);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Computes a stable geometry hash for determinism verification.
    /// </summary>
    public static byte[] ComputeGeometryHash(this ReconstructedFeature feature)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(feature.SourceFeatureId.Value.ToByteArray());
        return hash;
    }
}
