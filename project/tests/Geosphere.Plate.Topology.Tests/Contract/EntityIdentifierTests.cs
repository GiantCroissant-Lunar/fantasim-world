using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Contract;

/// <summary>
/// Unit tests for stable entity identifiers (PlateId, BoundaryId, JunctionId) per FR-005.
/// </summary>
public class EntityIdentifierTests
{
    #region PlateId Tests

    [Fact]
    public void PlateId_NewId_CreatesNonEmptyIdentifier()
    {
        // Act
        var plateId = PlateId.NewId();

        // Assert
        Assert.False(plateId.IsEmpty);
        Assert.NotEqual(Guid.Empty, plateId.Value);
    }

    [Fact]
    public void PlateId_NewId_CreatesUniqueIdentifiers()
    {
        // Arrange
        var id1 = PlateId.NewId();
        var id2 = PlateId.NewId();
        var id3 = PlateId.NewId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void PlateId_NewId_ProducesUUIDv7()
    {
        // Act
        var plateId = PlateId.NewId();

        // Assert - Extract RFC bytes (reverse Guid byte mapping)
        var guidBytes = plateId.Value.ToByteArray();
        var rfcBytes = new byte[16];
        rfcBytes[0] = guidBytes[3];
        rfcBytes[1] = guidBytes[2];
        rfcBytes[2] = guidBytes[1];
        rfcBytes[3] = guidBytes[0];
        rfcBytes[4] = guidBytes[5];
        rfcBytes[5] = guidBytes[4];
        rfcBytes[6] = guidBytes[7];
        rfcBytes[7] = guidBytes[6];
        Buffer.BlockCopy(guidBytes, 8, rfcBytes, 8, 8);

        // Verify version is 7 (bits 4-7 of byte 6)
        var version = rfcBytes[6] >> 4;
        Assert.Equal(7, version);

        // Verify RFC4122 variant (bits 6-7 of byte 8: 0b10xxxxxx)
        Assert.True((rfcBytes[8] & 0xC0) == 0x80);
    }

    [Fact]
    public void PlateId_NewId_ProducesTimeSortedIds()
    {
        // Act
        var id1 = PlateId.NewId();
        System.Threading.Thread.Sleep(10); // Ensure time passes
        var id2 = PlateId.NewId();

        // Assert - Extract RFC bytes to compare timestamps
        var guidBytes1 = id1.Value.ToByteArray();
        var guidBytes2 = id2.Value.ToByteArray();

        var rfcBytes1 = new byte[16];
        var rfcBytes2 = new byte[16];

        rfcBytes1[0] = guidBytes1[3];
        rfcBytes1[1] = guidBytes1[2];
        rfcBytes1[2] = guidBytes1[1];
        rfcBytes1[3] = guidBytes1[0];
        rfcBytes1[4] = guidBytes1[5];
        rfcBytes1[5] = guidBytes1[4];
        Buffer.BlockCopy(guidBytes1, 6, rfcBytes1, 6, 10);

        rfcBytes2[0] = guidBytes2[3];
        rfcBytes2[1] = guidBytes2[2];
        rfcBytes2[2] = guidBytes2[1];
        rfcBytes2[3] = guidBytes2[0];
        rfcBytes2[4] = guidBytes2[5];
        rfcBytes2[5] = guidBytes2[4];
        Buffer.BlockCopy(guidBytes2, 6, rfcBytes2, 6, 10);

        // Compare the 48-bit timestamp (first 6 bytes)
        for (int i = 0; i < 6; i++)
        {
            var cmp = rfcBytes1[i].CompareTo(rfcBytes2[i]);
            if (cmp != 0)
            {
                Assert.True(cmp < 0, "Later ID should have greater or equal timestamp");
                return;
            }
        }
    }

    [Fact]
    public void PlateId_Parse_ValidGuid_ReturnsIdentifier()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var plateId = PlateId.Parse(expectedGuid.ToString());

        // Assert
        Assert.Equal(expectedGuid, plateId.Value);
    }

    [Fact]
    public void PlateId_Parse_InvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlateId.Parse("invalid-guid"));
    }

    [Fact]
    public void PlateId_Parse_NullString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlateId.Parse(null!));
    }

    [Fact]
    public void PlateId_Parse_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlateId.Parse(string.Empty));
    }

    [Fact]
    public void PlateId_Parse_WhitespaceString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlateId.Parse("   "));
    }

    [Fact]
    public void PlateId_Parse_GuidEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlateId.Parse(Guid.Empty.ToString()));
    }

    [Fact]
    public void PlateId_TryParse_ValidGuid_ReturnsTrue()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var success = PlateId.TryParse(expectedGuid.ToString(), out var plateId);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedGuid, plateId.Value);
    }

    [Fact]
    public void PlateId_TryParse_InvalidGuid_ReturnsFalse()
    {
        // Act
        var success = PlateId.TryParse("invalid-guid", out var plateId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(PlateId), plateId);
    }

    [Fact]
    public void PlateId_TryParse_NullString_ReturnsFalse()
    {
        // Act
        var success = PlateId.TryParse(null!, out var plateId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(PlateId), plateId);
    }

    [Fact]
    public void PlateId_TryParse_EmptyString_ReturnsFalse()
    {
        // Act
        var success = PlateId.TryParse(string.Empty, out var plateId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(PlateId), plateId);
    }

    [Fact]
    public void PlateId_TryParse_WhitespaceString_ReturnsFalse()
    {
        // Act
        var success = PlateId.TryParse("   ", out var plateId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(PlateId), plateId);
    }

    [Fact]
    public void PlateId_TryParse_GuidEmpty_ReturnsFalse()
    {
        // Act
        var success = PlateId.TryParse(Guid.Empty.ToString(), out var plateId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(PlateId), plateId);
    }

    [Fact]
    public void PlateId_Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new PlateId(guid);
        var id2 = new PlateId(guid);

        // Act & Assert
        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void PlateId_Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = PlateId.NewId();
        var id2 = PlateId.NewId();

        // Act & Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void PlateId_ToString_ReturnsFormattedGuid()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var plateId = new PlateId(guid);

        // Act
        var result = plateId.ToString();

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", result);
    }

    [Fact]
    public void PlateId_DefaultValue_IsEmpty()
    {
        // Arrange
        var plateId = default(PlateId);

        // Act & Assert
        Assert.True(plateId.IsEmpty);
    }

    #endregion

    #region BoundaryId Tests

    [Fact]
    public void BoundaryId_NewId_CreatesNonEmptyIdentifier()
    {
        // Act
        var boundaryId = BoundaryId.NewId();

        // Assert
        Assert.False(boundaryId.IsEmpty);
        Assert.NotEqual(Guid.Empty, boundaryId.Value);
    }

    [Fact]
    public void BoundaryId_NewId_CreatesUniqueIdentifiers()
    {
        // Arrange
        var id1 = BoundaryId.NewId();
        var id2 = BoundaryId.NewId();
        var id3 = BoundaryId.NewId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void BoundaryId_NewId_ProducesUUIDv7()
    {
        // Act
        var boundaryId = BoundaryId.NewId();

        // Assert - Extract RFC bytes (reverse Guid byte mapping)
        var guidBytes = boundaryId.Value.ToByteArray();
        var rfcBytes = new byte[16];
        rfcBytes[0] = guidBytes[3];
        rfcBytes[1] = guidBytes[2];
        rfcBytes[2] = guidBytes[1];
        rfcBytes[3] = guidBytes[0];
        rfcBytes[4] = guidBytes[5];
        rfcBytes[5] = guidBytes[4];
        rfcBytes[6] = guidBytes[7];
        rfcBytes[7] = guidBytes[6];
        Buffer.BlockCopy(guidBytes, 8, rfcBytes, 8, 8);

        // Verify version is 7 (bits 4-7 of byte 6)
        var version = rfcBytes[6] >> 4;
        Assert.Equal(7, version);

        // Verify RFC4122 variant (bits 6-7 of byte 8: 0b10xxxxxx)
        Assert.True((rfcBytes[8] & 0xC0) == 0x80);
    }

    [Fact]
    public void BoundaryId_NewId_ProducesTimeSortedIds()
    {
        // Act
        var id1 = BoundaryId.NewId();
        System.Threading.Thread.Sleep(10); // Ensure time passes
        var id2 = BoundaryId.NewId();

        // Assert - Extract RFC bytes to compare timestamps
        var guidBytes1 = id1.Value.ToByteArray();
        var guidBytes2 = id2.Value.ToByteArray();

        var rfcBytes1 = new byte[16];
        var rfcBytes2 = new byte[16];

        rfcBytes1[0] = guidBytes1[3];
        rfcBytes1[1] = guidBytes1[2];
        rfcBytes1[2] = guidBytes1[1];
        rfcBytes1[3] = guidBytes1[0];
        rfcBytes1[4] = guidBytes1[5];
        rfcBytes1[5] = guidBytes1[4];
        Buffer.BlockCopy(guidBytes1, 6, rfcBytes1, 6, 10);

        rfcBytes2[0] = guidBytes2[3];
        rfcBytes2[1] = guidBytes2[2];
        rfcBytes2[2] = guidBytes2[1];
        rfcBytes2[3] = guidBytes2[0];
        rfcBytes2[4] = guidBytes2[5];
        rfcBytes2[5] = guidBytes2[4];
        Buffer.BlockCopy(guidBytes2, 6, rfcBytes2, 6, 10);

        // Compare the 48-bit timestamp (first 6 bytes)
        for (int i = 0; i < 6; i++)
        {
            var cmp = rfcBytes1[i].CompareTo(rfcBytes2[i]);
            if (cmp != 0)
            {
                Assert.True(cmp < 0, "Later ID should have greater or equal timestamp");
                return;
            }
        }
    }

    [Fact]
    public void BoundaryId_Parse_ValidGuid_ReturnsIdentifier()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var boundaryId = BoundaryId.Parse(expectedGuid.ToString());

        // Assert
        Assert.Equal(expectedGuid, boundaryId.Value);
    }

    [Fact]
    public void BoundaryId_Parse_InvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BoundaryId.Parse("invalid-guid"));
    }

    [Fact]
    public void BoundaryId_Parse_NullString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BoundaryId.Parse(null!));
    }

    [Fact]
    public void BoundaryId_Parse_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BoundaryId.Parse(string.Empty));
    }

    [Fact]
    public void BoundaryId_Parse_WhitespaceString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BoundaryId.Parse("   "));
    }

    [Fact]
    public void BoundaryId_Parse_GuidEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BoundaryId.Parse(Guid.Empty.ToString()));
    }

    [Fact]
    public void BoundaryId_TryParse_ValidGuid_ReturnsTrue()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var success = BoundaryId.TryParse(expectedGuid.ToString(), out var boundaryId);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedGuid, boundaryId.Value);
    }

    [Fact]
    public void BoundaryId_TryParse_InvalidGuid_ReturnsFalse()
    {
        // Act
        var success = BoundaryId.TryParse("invalid-guid", out var boundaryId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(BoundaryId), boundaryId);
    }

    [Fact]
    public void BoundaryId_TryParse_NullString_ReturnsFalse()
    {
        // Act
        var success = BoundaryId.TryParse(null!, out var boundaryId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(BoundaryId), boundaryId);
    }

    [Fact]
    public void BoundaryId_TryParse_EmptyString_ReturnsFalse()
    {
        // Act
        var success = BoundaryId.TryParse(string.Empty, out var boundaryId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(BoundaryId), boundaryId);
    }

    [Fact]
    public void BoundaryId_TryParse_WhitespaceString_ReturnsFalse()
    {
        // Act
        var success = BoundaryId.TryParse("   ", out var boundaryId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(BoundaryId), boundaryId);
    }

    [Fact]
    public void BoundaryId_TryParse_GuidEmpty_ReturnsFalse()
    {
        // Act
        var success = BoundaryId.TryParse(Guid.Empty.ToString(), out var boundaryId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(BoundaryId), boundaryId);
    }

    [Fact]
    public void BoundaryId_Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new BoundaryId(guid);
        var id2 = new BoundaryId(guid);

        // Act & Assert
        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void BoundaryId_Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = BoundaryId.NewId();
        var id2 = BoundaryId.NewId();

        // Act & Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void BoundaryId_ToString_ReturnsFormattedGuid()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var boundaryId = new BoundaryId(guid);

        // Act
        var result = boundaryId.ToString();

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", result);
    }

    #endregion

    #region JunctionId Tests

    [Fact]
    public void JunctionId_NewId_CreatesNonEmptyIdentifier()
    {
        // Act
        var junctionId = JunctionId.NewId();

        // Assert
        Assert.False(junctionId.IsEmpty);
        Assert.NotEqual(Guid.Empty, junctionId.Value);
    }

    [Fact]
    public void JunctionId_NewId_CreatesUniqueIdentifiers()
    {
        // Arrange
        var id1 = JunctionId.NewId();
        var id2 = JunctionId.NewId();
        var id3 = JunctionId.NewId();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void JunctionId_NewId_ProducesUUIDv7()
    {
        // Act
        var junctionId = JunctionId.NewId();

        // Assert - Extract RFC bytes (reverse Guid byte mapping)
        var guidBytes = junctionId.Value.ToByteArray();
        var rfcBytes = new byte[16];
        rfcBytes[0] = guidBytes[3];
        rfcBytes[1] = guidBytes[2];
        rfcBytes[2] = guidBytes[1];
        rfcBytes[3] = guidBytes[0];
        rfcBytes[4] = guidBytes[5];
        rfcBytes[5] = guidBytes[4];
        rfcBytes[6] = guidBytes[7];
        rfcBytes[7] = guidBytes[6];
        Buffer.BlockCopy(guidBytes, 8, rfcBytes, 8, 8);

        // Verify version is 7 (bits 4-7 of byte 6)
        var version = rfcBytes[6] >> 4;
        Assert.Equal(7, version);

        // Verify RFC4122 variant (bits 6-7 of byte 8: 0b10xxxxxx)
        Assert.True((rfcBytes[8] & 0xC0) == 0x80);
    }

    [Fact]
    public void JunctionId_NewId_ProducesTimeSortedIds()
    {
        // Act
        var id1 = JunctionId.NewId();
        System.Threading.Thread.Sleep(10); // Ensure time passes
        var id2 = JunctionId.NewId();

        // Assert - Extract RFC bytes to compare timestamps
        var guidBytes1 = id1.Value.ToByteArray();
        var guidBytes2 = id2.Value.ToByteArray();

        var rfcBytes1 = new byte[16];
        var rfcBytes2 = new byte[16];

        rfcBytes1[0] = guidBytes1[3];
        rfcBytes1[1] = guidBytes1[2];
        rfcBytes1[2] = guidBytes1[1];
        rfcBytes1[3] = guidBytes1[0];
        rfcBytes1[4] = guidBytes1[5];
        rfcBytes1[5] = guidBytes1[4];
        Buffer.BlockCopy(guidBytes1, 6, rfcBytes1, 6, 10);

        rfcBytes2[0] = guidBytes2[3];
        rfcBytes2[1] = guidBytes2[2];
        rfcBytes2[2] = guidBytes2[1];
        rfcBytes2[3] = guidBytes2[0];
        rfcBytes2[4] = guidBytes2[5];
        rfcBytes2[5] = guidBytes2[4];
        Buffer.BlockCopy(guidBytes2, 6, rfcBytes2, 6, 10);

        // Compare the 48-bit timestamp (first 6 bytes)
        for (int i = 0; i < 6; i++)
        {
            var cmp = rfcBytes1[i].CompareTo(rfcBytes2[i]);
            if (cmp != 0)
            {
                Assert.True(cmp < 0, "Later ID should have greater or equal timestamp");
                return;
            }
        }
    }

    [Fact]
    public void JunctionId_Parse_ValidGuid_ReturnsIdentifier()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var junctionId = JunctionId.Parse(expectedGuid.ToString());

        // Assert
        Assert.Equal(expectedGuid, junctionId.Value);
    }

    [Fact]
    public void JunctionId_Parse_InvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => JunctionId.Parse("invalid-guid"));
    }

    [Fact]
    public void JunctionId_Parse_NullString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => JunctionId.Parse(null!));
    }

    [Fact]
    public void JunctionId_Parse_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => JunctionId.Parse(string.Empty));
    }

    [Fact]
    public void JunctionId_Parse_WhitespaceString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => JunctionId.Parse("   "));
    }

    [Fact]
    public void JunctionId_Parse_GuidEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => JunctionId.Parse(Guid.Empty.ToString()));
    }

    [Fact]
    public void JunctionId_TryParse_ValidGuid_ReturnsTrue()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var success = JunctionId.TryParse(expectedGuid.ToString(), out var junctionId);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedGuid, junctionId.Value);
    }

    [Fact]
    public void JunctionId_TryParse_InvalidGuid_ReturnsFalse()
    {
        // Act
        var success = JunctionId.TryParse("invalid-guid", out var junctionId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(JunctionId), junctionId);
    }

    [Fact]
    public void JunctionId_TryParse_NullString_ReturnsFalse()
    {
        // Act
        var success = JunctionId.TryParse(null!, out var junctionId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(JunctionId), junctionId);
    }

    [Fact]
    public void JunctionId_TryParse_EmptyString_ReturnsFalse()
    {
        // Act
        var success = JunctionId.TryParse(string.Empty, out var junctionId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(JunctionId), junctionId);
    }

    [Fact]
    public void JunctionId_TryParse_WhitespaceString_ReturnsFalse()
    {
        // Act
        var success = JunctionId.TryParse("   ", out var junctionId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(JunctionId), junctionId);
    }

    [Fact]
    public void JunctionId_TryParse_GuidEmpty_ReturnsFalse()
    {
        // Act
        var success = JunctionId.TryParse(Guid.Empty.ToString(), out var junctionId);

        // Assert
        Assert.False(success);
        Assert.Equal(default(JunctionId), junctionId);
    }

    [Fact]
    public void JunctionId_Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new JunctionId(guid);
        var id2 = new JunctionId(guid);

        // Act & Assert
        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void JunctionId_Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = JunctionId.NewId();
        var id2 = JunctionId.NewId();

        // Act & Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void JunctionId_ToString_ReturnsFormattedGuid()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var junctionId = new JunctionId(guid);

        // Act
        var result = junctionId.ToString();

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", result);
    }

    #endregion

    #region Cross-Type Tests

    [Fact]
    public void DifferentIdentifierTypes_DoNotInterfere()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var plateId = new PlateId(guid);
        var boundaryId = new BoundaryId(guid);
        var junctionId = new JunctionId(guid);

        // Act & Assert - Each type is distinct even with same underlying Guid
        Assert.Equal(guid, plateId.Value);
        Assert.Equal(guid, boundaryId.Value);
        Assert.Equal(guid, junctionId.Value);

        // The record structs should not be equal across types
        Assert.False(plateId.Equals(boundaryId));
        Assert.False(boundaryId.Equals(junctionId));
        Assert.False(plateId.Equals(junctionId));
    }

    #endregion
}
