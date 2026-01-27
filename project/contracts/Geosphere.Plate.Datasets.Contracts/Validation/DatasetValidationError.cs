using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Validation;

[UnifyModel]
public sealed record DatasetValidationError(
    [property: UnifyProperty(0)] string Code,
    [property: UnifyProperty(1)] string Path,
    [property: UnifyProperty(2)] string Message);
