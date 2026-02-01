using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.World.Plates.Tests;

/// <summary>
/// Test gates for RFC-V2-0045 determinism requirements (Section 7).
/// </summary>
public class DeterminismTests
{
    #region Output Determinism Gate

    [Fact]
    public void Output_Determinism_Gate_ReconstructedFeatures_Are_Stably_Sorted()
    {
        // Arrange: Create features in non-deterministic order
        var features = new List<ReconstructedFeature>
        {
            CreateFeature(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            CreateFeature(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            CreateFeature(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        };

        // Act: Order canonically
        var ordered = features.OrderCanonically().ToList();

        // Assert: Features are sorted by SourceFeatureId.Value ascending
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), ordered[0].SourceFeatureId.Value);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), ordered[1].SourceFeatureId.Value);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), ordered[2].SourceFeatureId.Value);
        Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), ordered[3].SourceFeatureId.Value);
    }

    [Fact]
    public void Output_Determinism_Gate_IsCanonicallySorted_Detects_Unsorted()
    {
        // Arrange: Sorted features
        var sortedFeatures = new List<ReconstructedFeature>
        {
            CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            CreateFeature(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        };

        // Unsorted features
        var unsortedFeatures = new List<ReconstructedFeature>
        {
            CreateFeature(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        };

        // Assert
        Assert.True(sortedFeatures.IsCanonicallySorted());
        Assert.False(unsortedFeatures.IsCanonicallySorted());
    }

    [Fact]
    public void Output_Determinism_Gate_ReconstructResult_Validate_Enforces_Sorting()
    {
        // Arrange: Create unsorted features
        var unsortedFeatures = new List<ReconstructedFeature>
        {
            CreateFeature(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        };

        var result = new ReconstructResult
        {
            Features = unsortedFeatures,
            Provenance = CreateValidProvenance(),
            Metadata = QueryMetadata.ForCacheHit("test", "v1")
        };

        // Act & Assert: Validation should fail due to unsorted features
        Assert.False(result.Validate(ProvenanceStrictness.Strict));
    }

    #endregion

    #region Geometry Hash Stability Gate

    [Fact]
    public void Geometry_Hash_Stability_Gate_Feature_Hash_Is_Deterministic()
    {
        // Arrange
        var feature = CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        // Act
        var hash1 = feature.ComputeGeometryHash();
        var hash2 = feature.ComputeGeometryHash();

        // Assert: Same feature produces identical hash
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Geometry_Hash_Stability_Gate_Different_Features_Different_Hashes()
    {
        // Arrange
        var feature1 = CreateFeature(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var feature2 = CreateFeature(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        // Act
        var hash1 = feature1.ComputeGeometryHash();
        var hash2 = feature2.ComputeGeometryHash();

        // Assert: Different features produce different hashes
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Pagination Stability Gate

    [Fact]
    public void Pagination_Stability_Gate_ReconstructResult_Has_Continuation_Cursor()
    {
        // Arrange
        var result = new ReconstructResult
        {
            Features = new[] { CreateFeature(Guid.NewGuid()) },
            Provenance = CreateValidProvenance(),
            Metadata = QueryMetadata.ForCacheHit("test", "v1"),
            ContinuationCursor = "cursor123",
            TotalCount = 100
        };

        // Assert
        Assert.True(result.HasMore);
        Assert.Equal("cursor123", result.ContinuationCursor);
        Assert.Equal(100, result.TotalCount);
    }

    [Fact]
    public void Pagination_Stability_Gate_Empty_Cursor_Indicates_No_More_Results()
    {
        // Arrange
        var result = new ReconstructResult
        {
            Features = Array.Empty<ReconstructedFeature>(),
            Provenance = CreateValidProvenance(),
            Metadata = QueryMetadata.ForCacheHit("test", "v1"),
            ContinuationCursor = null
        };

        // Assert
        Assert.False(result.HasMore);
    }

    [Fact]
    public void Pagination_Stability_Gate_ReconstructOptions_PageSize_Configurable()
    {
        // Arrange & Act
        var options = ReconstructOptions.ForPagination(50, "cursor123");

        // Assert
        Assert.Equal(50, options.PageSize);
        Assert.Equal("cursor123", options.ContinuationCursor);
    }

    #endregion

    #region Parameter Canonicalization Gate

    [Fact]
    public void Parameter_Canonicalization_Gate_FeatureIds_Are_Deduplicated_And_Sorted()
    {
        // Arrange: Duplicate and unsorted IDs
        var ids = new[]
        {
            new FeatureId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            new FeatureId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            new FeatureId(Guid.Parse("33333333-3333-3333-3333-333333333333")), // duplicate
            new FeatureId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        };

        // Act
        var canonical = ids.Canonicalize();

        // Assert: Duplicates removed, sorted ascending
        Assert.Equal(3, canonical.Count);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), canonical[0].Value);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), canonical[1].Value);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), canonical[2].Value);
    }

    [Fact]
    public void Parameter_Canonicalization_Gate_PlateIds_Are_Deduplicated_And_Sorted()
    {
        // Arrange
        var ids = new[]
        {
            new PlateId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            new PlateId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            new PlateId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        };

        // Act
        var canonical = ids.Canonicalize();

        // Assert
        Assert.Equal(2, canonical.Count);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), canonical[0].Value);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), canonical[1].Value);
    }

    #endregion

    #region IEEE 754-2019 Compliance Gate

    [Fact]
    public void IEEE754_TotalOrder_NaN_Sorts_After_Numbers()
    {
        // Arrange
        var values = new[] { 3.0, double.NaN, 1.0, 2.0 };

        // Act
        var sorted = values.OrderBy(v => v, new Ieee754TotalOrderComparer()).ToList();

        // Assert: NaN should be at the end
        Assert.Equal(1.0, sorted[0]);
        Assert.Equal(2.0, sorted[1]);
        Assert.Equal(3.0, sorted[2]);
        Assert.True(double.IsNaN(sorted[3]));
    }

    [Fact]
    public void IEEE754_TotalOrder_NegativeZero_LessThan_PositiveZero()
    {
        // Arrange
        var negZero = -0.0;
        var posZero = 0.0;

        // Act
        var comparison = DeterminismHelpers.TotalOrderCompare(negZero, posZero);

        // Assert: -0.0 < +0.0 in total order
        Assert.True(comparison < 0);
    }

    [Fact]
    public void IEEE754_TotalOrder_Equal_Numbers()
    {
        // Act & Assert
        Assert.Equal(0, DeterminismHelpers.TotalOrderCompare(1.0, 1.0));
        Assert.Equal(0, DeterminismHelpers.TotalOrderCompare(-0.0, -0.0));
        Assert.Equal(0, DeterminismHelpers.TotalOrderCompare(double.NaN, double.NaN));
    }

    #endregion

    #region Helper Methods

    private static ReconstructedFeature CreateFeature(Guid featureId)
    {
        return new ReconstructedFeature
        {
            SourceFeatureId = new FeatureId(featureId),
            PlateId = new PlateId(Guid.NewGuid()),
            Geometry = new Point2(0, 0)
        };
    }

    private static ProvenanceChain CreateValidProvenance()
    {
        return new ProvenanceChain
        {
            SourceFeatureIds = Array.Empty<FeatureId>(),
            SourceBoundaryIds = Array.Empty<BoundaryId>(),
            SourceJunctionIds = Array.Empty<JunctionId>(),
            KinematicsModelId = new ModelId(Guid.NewGuid()),
            KinematicsModelVersion = 1,
            RotationSegments = Array.Empty<RotationSegmentRef>(),
            TopologyStreamHash = new byte[32], // SHA-256 size
            TopologyReferenceTick = CanonicalTick.Genesis,
            QueryTick = CanonicalTick.Genesis,
            QueryContractVersion = "RFC-V2-0045",
            SolverImplementation = "TestSolver"
        };
    }

    #endregion
}

/// <summary>
/// Comparer for IEEE 754-2019 total ordering of double values.
/// </summary>
internal sealed class Ieee754TotalOrderComparer : IComparer<double>
{
    public int Compare(double x, double y)
    {
        return DeterminismHelpers.TotalOrderCompare(x, y);
    }
}
