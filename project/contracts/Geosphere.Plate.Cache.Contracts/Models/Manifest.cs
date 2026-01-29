using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// Manifest metadata record describing a derived artifact.
///
/// Required Fields:
/// - schema_version (int): Schema version, starts at 1
/// - product_type (str): Must equal product type in the key
/// - input_fingerprint (str): Must equal fingerprint in the key
/// - source_stream (str): Full stream identity string
/// - boundary (map): Boundary specification
/// - generator (map): Generator specification
/// - params_hash (str): Lowercase hex SHA-256 of params
/// - storage (map): Storage specification
///
/// Optional Fields:
/// - created_at_utc (str): ISO-8601 UTC timestamp
/// - meta (map): Freeform metadata
/// - external (map): External storage details
/// - params (map): Full params object (for debugging)
/// </summary>
[MessagePackObject]
public readonly record struct Manifest(
    // Required fields
    [property: Key(0)] int SchemaVersion,
    [property: Key(1)] string ProductType,
    [property: Key(2)] string InputFingerprint,
    [property: Key(3)] string SourceStream,
    [property: Key(4)] Boundary Boundary,
    [property: Key(5)] GeneratorInfo Generator,
    [property: Key(6)] string ParamsHash,
    [property: Key(7)] StorageInfo Storage,

    // Optional fields (nullable)
    [property: Key(8)] string? CreatedAtUtc,
    [property: Key(9)] Dictionary<string, object>? Meta,
    [property: Key(10)] ExternalStorageInfo? External,
    [property: Key(11)] Dictionary<string, object>? Params
)
{
    /// <summary>
    /// Current manifest schema version.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Creates a new manifest with required fields.
    /// </summary>
    public static Manifest Create(
        string productType,
        string inputFingerprint,
        string sourceStream,
        Boundary boundary,
        GeneratorInfo generator,
        string paramsHash,
        StorageInfo storage)
    {
        return new Manifest(
            SchemaVersion: CurrentSchemaVersion,
            ProductType: productType,
            InputFingerprint: inputFingerprint,
            SourceStream: sourceStream,
            Boundary: boundary,
            Generator: generator,
            ParamsHash: paramsHash,
            Storage: storage,
            CreatedAtUtc: null,
            Meta: null,
            External: null,
            Params: null
        );
    }

    /// <summary>
    /// Validates the manifest according to the schema.
    /// </summary>
    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentException($"SchemaVersion must be {CurrentSchemaVersion}", nameof(SchemaVersion));

        if (string.IsNullOrWhiteSpace(ProductType))
            throw new ArgumentException("ProductType cannot be null or empty", nameof(ProductType));

        if (string.IsNullOrWhiteSpace(InputFingerprint))
            throw new ArgumentException("InputFingerprint cannot be null or empty", nameof(InputFingerprint));

        // Validate input_fingerprint format (64 lowercase hex characters)
        if (InputFingerprint.Length != 64)
            throw new ArgumentException("InputFingerprint must be 64 characters (SHA-256 hex)", nameof(InputFingerprint));

        foreach (var c in InputFingerprint)
        {
            if (!IsLowercaseHexChar(c))
                throw new ArgumentException("InputFingerprint must be lowercase hexadecimal", nameof(InputFingerprint));
        }

        if (string.IsNullOrWhiteSpace(SourceStream))
            throw new ArgumentException("SourceStream cannot be null or empty", nameof(SourceStream));

        Boundary.Validate();
        Generator.Validate();

        if (string.IsNullOrWhiteSpace(ParamsHash))
            throw new ArgumentException("ParamsHash cannot be null or empty", nameof(ParamsHash));

        // Validate params_hash format
        if (ParamsHash.Length != 64)
            throw new ArgumentException("ParamsHash must be 64 characters (SHA-256 hex)", nameof(ParamsHash));

        foreach (var c in ParamsHash)
        {
            if (!IsLowercaseHexChar(c))
                throw new ArgumentException("ParamsHash must be lowercase hexadecimal", nameof(ParamsHash));
        }

        Storage.Validate();

        // If storage mode is external, external info is required
        if (Storage.Mode == StorageMode.External && External == null)
            throw new ArgumentException("External storage info is required when Storage.Mode is External", nameof(External));

        External?.Validate();
    }

    private static bool IsLowercaseHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
}
