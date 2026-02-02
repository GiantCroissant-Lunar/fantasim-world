namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public readonly record struct DerivedProductLookupResult<T>
{
    public bool IsHit { get; init; }

    public T? Product { get; init; }

    public DerivedProductProvenance? Provenance { get; init; }

    public string? ProductInstanceId { get; init; }

    public static DerivedProductLookupResult<T> Hit(
        T product,
        DerivedProductProvenance provenance,
        string productInstanceId)
    {
        return new DerivedProductLookupResult<T>
        {
            IsHit = true,
            Product = product,
            Provenance = provenance,
            ProductInstanceId = productInstanceId
        };
    }

    public static DerivedProductLookupResult<T> Computed(
        T product,
        DerivedProductProvenance provenance,
        string productInstanceId)
    {
        return new DerivedProductLookupResult<T>
        {
            IsHit = false,
            Product = product,
            Provenance = provenance,
            ProductInstanceId = productInstanceId
        };
    }
}
