using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Manifest;

public enum BodyShape
{
    Sphere = 0,
    Ellipsoid = 1
}

[UnifyModel]
public sealed record BodyFrame(
    [property: UnifyProperty(0)] BodyShape Shape,
    [property: UnifyProperty(1)] double? Radius,
    [property: UnifyProperty(2)] double? SemiMajor,
    [property: UnifyProperty(3)] double? SemiMinor,
    [property: UnifyProperty(4)] string Unit,
    [property: UnifyProperty(5)] string AngularConvention
);
