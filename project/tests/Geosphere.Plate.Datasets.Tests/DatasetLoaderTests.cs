using System.IO;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Loading;
using FantaSim.Geosphere.Plate.Datasets.Import;
using FluentAssertions;

namespace FantaSim.Geosphere.Plate.Datasets.Tests;

public sealed class DatasetLoaderTests
{
    [Fact]
    public async Task LoadAsync_ValidSphereManifest_LoadsAndOrdersAssetsDeterministically()
    {
        var loader = new JsonPlatesDatasetLoader();
        var datasetRoot = GetSampleRoot("ValidSphere");

        var r1 = await loader.LoadAsync(datasetRoot, null, CancellationToken.None);
        var r2 = await loader.LoadAsync(datasetRoot, null, CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();

        r1.Dataset!.Manifest.DatasetId.Should().Be("test.dataset");
        r1.Dataset.Manifest.BodyFrame.Shape.Should().Be(Contracts.Manifest.BodyShape.Sphere);

        r1.Dataset.Assets.Should().Equal(r2.Dataset!.Assets);

        // Assert deterministic ordering: Kind, AssetId
        r1.Dataset.Assets.Select(a => a.AssetId).Should().Equal(new[] { "a", "b" });
    }

    [Fact]
    public async Task LoadAsync_MissingBodyFrame_FailsWithDeterministicError()
    {
        var loader = new JsonPlatesDatasetLoader();
        var datasetRoot = GetSampleRoot("MissingBodyFrame");

        var r = await loader.LoadAsync(datasetRoot, null, CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        r.Errors[0].Code.Should().Be("body_frame.required");
    }

    [Fact]
    public async Task LoadAsync_MissingAssetFile_FailsWithDeterministicError()
    {
        var loader = new JsonPlatesDatasetLoader();
        var datasetRoot = GetSampleRoot("MissingAssetFile");

        var r = await loader.LoadAsync(datasetRoot, null, CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        r.Errors.Any(e => e.Code == "asset_file.missing").Should().BeTrue();
    }

    private static string GetSampleRoot(string folderName)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "SampleData", folderName);
    }
}
