using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Contract;

/// <summary>
/// Unit tests for Domain identity contract.
/// </summary>
public class DomainTests
{
    [Fact]
    public void Parse_ValidDomain_ReturnsDomain()
    {
        // Act
        var domain = Domain.Parse("geo.plates");

        // Assert
        Assert.Equal("geo.plates", domain.Value);
        Assert.True(domain.IsValid());
    }

    [Fact]
    public void Parse_ValidDomainWithUnderscore_ReturnsDomain()
    {
        // Act
        var domain = Domain.Parse("geo.plates_v1");

        // Assert
        Assert.Equal("geo.plates_v1", domain.Value);
    }

    [Fact]
    public void Parse_ValidDomainWithMultipleSegments_ReturnsDomain()
    {
        // Act
        var domain = Domain.Parse("geo.tectonic.plates");

        // Assert
        Assert.Equal("geo.tectonic.plates", domain.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullEmptyOrWhitespace_ThrowsArgumentException(string? value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Domain.Parse(value!));
    }

    [Theory]
    [InlineData("geo plates")] // Space
    [InlineData("geo/plates")] // Slash
    [InlineData("geo-plates")] // Hyphen
    [InlineData("geo:plates")] // Colon
    public void Parse_InvalidCharacters_ThrowsArgumentException(string value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Domain.Parse(value));
    }

    [Theory]
    [InlineData("geo..plates")] // Consecutive dots
    [InlineData(".geo.plates")] // Leading dot
    [InlineData("geo.plates.")] // Trailing dot
    public void Parse_InvalidDotPlacement_ThrowsArgumentException(string value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Domain.Parse(value));
    }

    [Fact]
    public void TryParse_ValidDomain_ReturnsTrueAndDomain()
    {
        // Act
        var result = Domain.TryParse("geo.plates", out var domain);

        // Assert
        Assert.True(result);
        Assert.Equal("geo.plates", domain.Value);
    }

    [Fact]
    public void TryParse_InvalidDomain_ReturnsFalseAndDefault()
    {
        // Act
        var result = Domain.TryParse("geo plates", out var domain);

        // Assert
        Assert.False(result);
        Assert.Equal(default, domain);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var domain = Domain.Parse("geo.plates");

        // Act
        var result = domain.ToString();

        // Assert
        Assert.Equal("geo.plates", result);
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsValue()
    {
        // Arrange
        var domain = Domain.Parse("geo.plates");

        // Act
        string result = domain;

        // Assert
        Assert.Equal("geo.plates", result);
    }

    [Fact]
    public void ExplicitStringConversion_ParsesCorrectly()
    {
        // Act
        Domain domain = (Domain)"geo.plates";

        // Assert
        Assert.Equal("geo.plates", domain.Value);
    }

    [Fact]
    public void ExplicitStringConversion_InvalidInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => (Domain)"geo plates");
    }

    [Fact]
    public void Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var domain1 = Domain.Parse("geo.plates");
        var domain2 = Domain.Parse("geo.plates");

        // Assert
        Assert.True(domain1.Equals(domain2));
        Assert.Equal(domain1.GetHashCode(), domain2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var domain1 = Domain.Parse("geo.plates");
        var domain2 = Domain.Parse("geo.rivers");

        // Assert
        Assert.False(domain1.Equals(domain2));
    }

    [Fact]
    public void DefaultDomain_IsNotValid()
    {
        // Arrange
        var domain = default(Domain);

        // Assert
        Assert.False(domain.IsValid());
        Assert.Equal(string.Empty, domain.ToString());
    }
}

/// <summary>
/// Unit tests for TruthStreamIdentity contract.
/// </summary>
public class TruthStreamIdentityTests
{
    [Fact]
    public void TruthStreamIdentity_ValidComponents_IsValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.True(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_NullVariantId_IsNotValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            null!,
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_EmptyBranchId_IsNotValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            string.Empty,
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_NegativeLLevel_IsNotValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            -1,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_ZeroLLevel_IsValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            0,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.True(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_InvalidDomain_IsNotValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            default(Domain),
            "M0"
        );

        // Act & Assert
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void TruthStreamIdentity_WhitespaceModel_IsNotValid()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "   "
        );

        // Act & Assert
        Assert.False(identity.IsValid());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act
        var result = identity.ToString();

        // Assert
        Assert.Equal("urn:fantasim:science:main:L2:geo.plates:M0", result);
    }

    [Fact]
    public void ToStreamKey_ReturnsDeterministicKey()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act
        var result = identity.ToStreamKey();

        // Assert
        Assert.Equal("science:main:L2:geo.plates:M0", result);
    }

    [Fact]
    public void Equality_SameComponents_ReturnsTrue()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.Equal(identity1, identity2);
        Assert.Equal(identity1.GetHashCode(), identity2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentVariant_ReturnsFalse()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "wuxing",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentBranch_ReturnsFalse()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "science",
            "truth",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentLLevel_ReturnsFalse()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "science",
            "main",
            3,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentDomain_ReturnsFalse()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.rivers"),
            "M0"
        );

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentModel_ReturnsFalse()
    {
        // Arrange
        var identity1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identity2 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M1"
        );

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void WithClauseSyntax_ModifiesIdentity()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act
        var modified = identity with { BranchId = "truth" };

        // Assert
        Assert.Equal("science", modified.VariantId);
        Assert.Equal("truth", modified.BranchId);
        Assert.Equal(2, modified.LLevel);
        Assert.Equal("geo.plates", modified.Domain.Value);
        Assert.Equal("M0", modified.Model);
    }

    [Fact]
    public void StreamKey_IsDeterministicAcrossMultipleCalls()
    {
        // Arrange
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );

        // Act
        var key1 = identity.ToStreamKey();
        var key2 = identity.ToStreamKey();

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ModelNormalization_M0AndZero_FormatIdentically()
    {
        // Arrange
        var identityWithPrefix = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "M0"
        );
        var identityWithoutPrefix = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );

        // Act
        var toString1 = identityWithPrefix.ToString();
        var toString2 = identityWithoutPrefix.ToString();
        var streamKey1 = identityWithPrefix.ToStreamKey();
        var streamKey2 = identityWithoutPrefix.ToStreamKey();

        // Assert - Both should format identically with single 'M' prefix
        Assert.Equal("urn:fantasim:science:main:L2:geo.plates:M0", toString1);
        Assert.Equal("urn:fantasim:science:main:L2:geo.plates:M0", toString2);
        Assert.Equal(toString1, toString2);

        Assert.Equal("science:main:L2:geo.plates:M0", streamKey1);
        Assert.Equal("science:main:L2:geo.plates:M0", streamKey2);
        Assert.Equal(streamKey1, streamKey2);
    }
}
