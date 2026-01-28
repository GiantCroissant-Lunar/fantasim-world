using FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;
using FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;
using FluentAssertions;

namespace FantaSim.Geosphere.Plate.Datasets.Tests.Validation;

/// <summary>
/// Tests for <see cref="PlatesDatasetManifestValidator"/> focusing on RFC-V2-0032 compliance.
/// </summary>
public sealed class ManifestValidatorTests
{
    #region Angular Convention Validation (RFC-V2-0032 §2.1)

    [Theory]
    [InlineData("body_lonlat_degrees")]
    [InlineData("body_lonlat_radians")]
    [InlineData("body_cartesian")]
    [InlineData("body-lonlat-degrees-v1")]
    [InlineData("body-lonlat-radians-v1")]
    [InlineData("body-cartesian-v1")]
    [InlineData("planetographic-lonlat-degrees-v1")]
    [InlineData("planetocentric-lonlat-degrees-v1")]
    public void Validate_AllowedAngularConvention_Passes(string convention)
    {
        var manifest = CreateValidManifest(angularConvention: convention);

        var errors = PlatesDatasetManifestValidator.Validate(manifest);

        errors.Should().NotContain(e => e.Code.Contains("angular_convention"));
    }

    [Theory]
    [InlineData("epsg:4326", "earth_crs_rejected")]
    [InlineData("EPSG:4326", "earth_crs_rejected")]
    [InlineData("wgs84", "earth_crs_rejected")]
    [InlineData("WGS84", "earth_crs_rejected")]
    [InlineData("wgs-84", "earth_crs_rejected")]
    [InlineData("crs84", "earth_crs_rejected")]
    [InlineData("ogc:crs84", "earth_crs_rejected")]
    [InlineData("urn:ogc:def:crs:EPSG::4326", "earth_crs_rejected")]
    [InlineData("proj:longlat", "earth_crs_rejected")]
    public void Validate_ImplicitEarthCrs_Rejected(string convention, string expectedErrorSuffix)
    {
        var manifest = CreateValidManifest(angularConvention: convention);

        var errors = PlatesDatasetManifestValidator.Validate(manifest);

        errors.Should().Contain(e => e.Code == $"body_frame.angular_convention.{expectedErrorSuffix}");
    }

    [Theory]
    [InlineData("unknown_convention")]
    [InlineData("lat_lon_degrees")]
    [InlineData("geodetic")]
    public void Validate_UnknownAngularConvention_Invalid(string convention)
    {
        var manifest = CreateValidManifest(angularConvention: convention);

        var errors = PlatesDatasetManifestValidator.Validate(manifest);

        errors.Should().Contain(e => e.Code == "body_frame.angular_convention.invalid");
    }

    #endregion

    #region Unit Validation

    [Theory]
    [InlineData("m")]
    [InlineData("km")]
    [InlineData("au")]
    [InlineData("mi")]
    [InlineData("nmi")]
    [InlineData("M")] // Case insensitive
    [InlineData("KM")]
    public void Validate_AllowedUnit_Passes(string unit)
    {
        var manifest = CreateValidManifest(unit: unit);

        var errors = PlatesDatasetManifestValidator.Validate(manifest);

        errors.Should().NotContain(e => e.Code.Contains("unit.invalid"));
    }

    [Theory]
    [InlineData("feet")]
    [InlineData("parsec")]
    [InlineData("unknown")]
    public void Validate_UnknownUnit_Invalid(string unit)
    {
        var manifest = CreateValidManifest(unit: unit);

        var errors = PlatesDatasetManifestValidator.Validate(manifest);

        errors.Should().Contain(e => e.Code == "body_frame.unit.invalid");
    }

    #endregion

    #region Helpers

    private static PlatesDatasetManifest CreateValidManifest(
        string? angularConvention = "body_lonlat_degrees",
        string? unit = "m")
    {
        return new PlatesDatasetManifest(
            DatasetId: "test.dataset",
            BodyId: "test.body",
            BodyFrame: new BodyFrame(
                Shape: BodyShape.Sphere,
                Radius: 1.0,
                SemiMajor: null,
                SemiMinor: null,
                Unit: unit!,
                AngularConvention: angularConvention!),
            TimeMapping: new TimeMapping(TickUnit: "CTU"),
            CanonicalizationRules: new CanonicalizationRules(
                Version: 1,
                StableIdPolicyId: "sha256-guid-v1",
                AssetOrderingPolicyId: "kind_assetId_path-v1",
                QuantizationPolicyId: "euler_microdegrees-v1"),
            FeatureSets: null,
            RasterSequences: null,
            MotionModels: null);
    }

    #endregion
}
