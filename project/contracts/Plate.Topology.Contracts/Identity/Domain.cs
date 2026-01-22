namespace Plate.Topology.Contracts.Identity;

/// <summary>
/// Stable domain identifier for truth streams.
///
/// Domains SHOULD be stable and specific (e.g., "geo.plates").
/// Domain identifiers are case-sensitive and must follow a dot-notation convention.
/// </summary>
public readonly record struct Domain
{
    private readonly string _value;

    /// <summary>
    /// The domain identifier value (e.g., "geo.plates").
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Private constructor to enforce validation via Parse.
    /// </summary>
    private Domain(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Parses a domain identifier string into a Domain struct.
    /// Valid domain identifiers are non-empty and contain only alphanumeric characters, dots, and underscores.
    /// </summary>
    /// <param name="value">The domain identifier string to parse.</param>
    /// <returns>A Domain struct.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, whitespace, or contains invalid characters.</exception>
    public static Domain Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Domain identifier cannot be null, empty, or whitespace.", nameof(value));

        // Validate format: alphanumeric, dots, underscores only
        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
                throw new ArgumentException($"Domain identifier contains invalid character: '{c}'. Only alphanumeric characters, dots, and underscores are allowed.", nameof(value));
        }

        // Prevent consecutive dots or leading/trailing dots
        if (value.Contains("..") || value.StartsWith('.') || value.EndsWith('.'))
            throw new ArgumentException("Domain identifier cannot contain consecutive dots, or start/end with a dot.", nameof(value));

        return new Domain(value);
    }

    /// <summary>
    /// Attempts to parse a domain identifier string into a Domain struct.
    /// </summary>
    /// <param name="value">The domain identifier string to parse.</param>
    /// <param name="domain">When this method returns, contains the parsed Domain if parsing succeeded.</param>
    /// <returns>true if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string value, out Domain domain)
    {
        try
        {
            domain = Parse(value);
            return true;
        }
        catch
        {
            domain = default;
            return false;
        }
    }

    /// <summary>
    /// Returns the string representation of the domain identifier.
    /// </summary>
    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    /// <summary>
    /// Validates that the domain identifier is well-formed.
    /// Applies the same validation rules as Parse():
    /// - Non-empty and non-whitespace
    /// - Contains only alphanumeric characters, dots, and underscores
    /// - No consecutive dots, leading dots, or trailing dots
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(_value))
            return false;

        // Check for invalid characters
        foreach (char c in _value)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
                return false;
        }

        // Check for consecutive dots or leading/trailing dots
        if (_value.Contains("..") || _value.StartsWith('.') || _value.EndsWith('.'))
            return false;

        return true;
    }

    /// <summary>
    /// Implicit conversion from Domain to string for convenience.
    /// </summary>
    public static implicit operator string(Domain domain) => domain._value;

    /// <summary>
    /// Explicit conversion from string to Domain (throws on invalid input).
    /// </summary>
    public static explicit operator Domain(string value) => Parse(value);

    /// <summary>
    /// Equality comparison for Domain structs.
    /// </summary>
    public bool Equals(Domain other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Hash code computation for Domain structs.
    /// </summary>
    public override int GetHashCode()
    {
        return _value?.GetHashCode(StringComparison.Ordinal) ?? 0;
    }
}
