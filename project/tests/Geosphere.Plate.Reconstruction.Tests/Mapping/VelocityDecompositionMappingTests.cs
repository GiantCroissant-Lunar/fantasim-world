using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Generated;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Mapping;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FluentAssertions;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests.Mapping;

public class VelocityDecompositionMappingTests
{
    private readonly VelocityDecompositionMapper _mapper = new();

    #region DTO → Domain Mapping Tests

    [Fact]
    public void ToDomain_MantleFrame_RoundtripsCorrectly()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 1.0, Y = 2.0, Z = 0.0 },
            DeformationComponent = null,
            RelativeToFrame = new() { Type = "mantle" },
            Magnitude = 2.236,
            Azimuth = 1.107
        };

        var domain = _mapper.ToDomain(dto);

        domain.RigidRotationComponent.X.Should().Be(1.0);
        domain.RigidRotationComponent.Y.Should().Be(2.0);
        domain.RigidRotationComponent.Z.Should().Be(0.0);
        domain.DeformationComponent.Should().BeNull();
        domain.RelativeToFrame.Should().BeOfType<MantleFrame>();
        domain.Magnitude.Should().Be(2.236);
        domain.Azimuth.Should().Be(1.107);
    }

    [Fact]
    public void ToDomain_PlateAnchorFrame_RoundtripsCorrectly()
    {
        var plateId = Guid.NewGuid();
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.5, Y = -0.3, Z = 0.1 },
            DeformationComponent = new() { X = 0.01, Y = 0.02, Z = 0.0 },
            RelativeToFrame = new()
            {
                Type = "plateAnchor",
                PlateId = plateId.ToString()
            },
            Magnitude = 0.583,
            Azimuth = -0.540
        };

        var domain = _mapper.ToDomain(dto);

        domain.RigidRotationComponent.X.Should().Be(0.5);
        domain.RigidRotationComponent.Y.Should().Be(-0.3);
        domain.RigidRotationComponent.Z.Should().Be(0.1);
        domain.DeformationComponent.Should().NotBeNull();
        domain.DeformationComponent!.Value.X.Should().Be(0.01);
        domain.DeformationComponent!.Value.Y.Should().Be(0.02);
        domain.DeformationComponent!.Value.Z.Should().Be(0.0);
        domain.RelativeToFrame.Should().BeOfType<PlateAnchor>();
        ((PlateAnchor)domain.RelativeToFrame).PlateId.Value.Should().Be(plateId);
        domain.Magnitude.Should().Be(0.583);
        domain.Azimuth.Should().Be(-0.540);
    }

    [Fact]
    public void ToDomain_AbsoluteFrame_RoundtripsCorrectly()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.0, Y = 0.0, Z = 3.5 },
            DeformationComponent = null,
            RelativeToFrame = new() { Type = "absolute" },
            Magnitude = 3.5,
            Azimuth = Math.PI / 2
        };

        var domain = _mapper.ToDomain(dto);

        domain.RelativeToFrame.Should().BeOfType<AbsoluteFrame>();
        domain.Magnitude.Should().Be(3.5);
    }

    [Fact]
    public void ToDomain_CustomFrameWithChain_RoundtripsCorrectly()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 1.0, Y = 1.0, Z = 1.0 },
            DeformationComponent = null,
            RelativeToFrame = new()
            {
                Type = "custom",
                Definition = new()
                {
                    Name = "TestFrame",
                    Chain = new List<FrameChainLinkDto>
                    {
                        new()
                        {
                            BaseFrame = new() { Type = "mantle" },
                            Transform = new() { W = 1.0, X = 0.0, Y = 0.0, Z = 0.0 },
                            ValidityRange = new() { StartTick = 0, EndTick = 1000 },
                            SequenceHint = 1
                        }
                    },
                    Metadata = new() { Description = "A test frame", Author = "TestSuite" }
                }
            },
            Magnitude = 1.732,
            Azimuth = 0.785
        };

        var domain = _mapper.ToDomain(dto);

        var custom = domain.RelativeToFrame.Should().BeOfType<CustomFrame>().Subject;
        custom.Definition.Name.Should().Be("TestFrame");
        custom.Definition.Chain.Should().HaveCount(1);
        custom.Definition.Chain[0].BaseFrame.Should().BeOfType<MantleFrame>();
        custom.Definition.Chain[0].Transform.Orientation.W.Should().Be(1.0);
        custom.Definition.Chain[0].ValidityRange.Should().NotBeNull();
        custom.Definition.Chain[0].ValidityRange!.Value.StartTick.Value.Should().Be(0);
        custom.Definition.Chain[0].ValidityRange!.Value.EndTick.Value.Should().Be(1000);
        custom.Definition.Chain[0].SequenceHint.Should().Be(1);
        custom.Definition.Metadata.Should().NotBeNull();
        custom.Definition.Metadata!.Description.Should().Be("A test frame");
        custom.Definition.Metadata!.Author.Should().Be("TestSuite");
    }

    #endregion

    #region Domain → DTO Mapping Tests

    [Fact]
    public void ToDto_MantleFrame_RoundtripsCorrectly()
    {
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(1.0, 2.0, 0.0),
            DeformationComponent = null,
            RelativeToFrame = MantleFrame.Instance,
            Magnitude = 2.236,
            Azimuth = 1.107
        };

        var dto = _mapper.ToDto(domain);

        dto.RelativeToFrame.Type.Should().Be("mantle");
        dto.RelativeToFrame.PlateId.Should().BeNull();
        dto.RelativeToFrame.Definition.Should().BeNull();
        dto.RigidRotationComponent.X.Should().Be(1.0);
        dto.RigidRotationComponent.Y.Should().Be(2.0);
        dto.RigidRotationComponent.Z.Should().Be(0.0);
    }

    [Fact]
    public void ToDto_PlateAnchorFrame_RoundtripsCorrectly()
    {
        var plateId = new PlateId(Guid.Parse("12345678-1234-1234-1234-123456789abc"));
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(0.5, -0.3, 0.1),
            DeformationComponent = new(0.01, 0.02, 0.0),
            RelativeToFrame = new PlateAnchor { PlateId = plateId },
            Magnitude = 0.583,
            Azimuth = -0.540
        };

        var dto = _mapper.ToDto(domain);

        dto.RelativeToFrame.Type.Should().Be("plateAnchor");
        dto.RelativeToFrame.PlateId.Should().Be("12345678-1234-1234-1234-123456789abc");
        dto.RelativeToFrame.Definition.Should().BeNull();
    }

    [Fact]
    public void ToDto_AbsoluteFrame_RoundtripsCorrectly()
    {
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(0.0, 0.0, 3.5),
            DeformationComponent = null,
            RelativeToFrame = AbsoluteFrame.Instance,
            Magnitude = 3.5,
            Azimuth = Math.PI / 2
        };

        var dto = _mapper.ToDto(domain);

        dto.RelativeToFrame.Type.Should().Be("absolute");
        dto.RelativeToFrame.PlateId.Should().BeNull();
        dto.RelativeToFrame.Definition.Should().BeNull();
    }

    #endregion

    #region Roundtrip Tests (DTO → Domain → DTO)

    [Fact]
    public void Roundtrip_MantleFrame_preservesAllValues()
    {
        var originalDto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 1.5, Y = -2.5, Z = 0.75 },
            DeformationComponent = null,
            RelativeToFrame = new() { Type = "mantle" },
            Magnitude = 3.041,
            Azimuth = -1.030
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.RelativeToFrame.Type.Should().Be(originalDto.RelativeToFrame.Type);
        roundtripDto.RigidRotationComponent.X.Should().Be(originalDto.RigidRotationComponent.X);
        roundtripDto.RigidRotationComponent.Y.Should().Be(originalDto.RigidRotationComponent.Y);
        roundtripDto.RigidRotationComponent.Z.Should().Be(originalDto.RigidRotationComponent.Z);
        roundtripDto.DeformationComponent.Should().BeNull();
        roundtripDto.Magnitude.Should().Be(originalDto.Magnitude);
        roundtripDto.Azimuth.Should().Be(originalDto.Azimuth);
    }

    [Fact]
    public void Roundtrip_PlateAnchorFrame_preservesAllValues()
    {
        var plateId = Guid.NewGuid();
        var originalDto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.5, Y = -0.3, Z = 0.1 },
            DeformationComponent = new() { X = 0.01, Y = 0.02, Z = 0.0 },
            RelativeToFrame = new()
            {
                Type = "plateAnchor",
                PlateId = plateId.ToString()
            },
            Magnitude = 0.583,
            Azimuth = -0.540
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.RelativeToFrame.Type.Should().Be("plateAnchor");
        roundtripDto.RelativeToFrame.PlateId.Should().Be(plateId.ToString());
        roundtripDto.RigidRotationComponent.X.Should().Be(0.5);
        roundtripDto.RigidRotationComponent.Y.Should().Be(-0.3);
        roundtripDto.RigidRotationComponent.Z.Should().Be(0.1);
        var deformation = roundtripDto.DeformationComponent.Should().NotBeNull().Subject;
        deformation.X.Should().Be(0.01);
        deformation.Y.Should().Be(0.02);
        deformation.Z.Should().Be(0.0);
        roundtripDto.Magnitude.Should().Be(0.583);
        roundtripDto.Azimuth.Should().Be(-0.540);
    }

    [Fact]
    public void Roundtrip_CustomFrameWithMetadata_preservesAllValues()
    {
        var originalDto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 1.0, Y = 0.0, Z = 0.0 },
            DeformationComponent = null,
            RelativeToFrame = new()
            {
                Type = "custom",
                Definition = new()
                {
                    Name = "EquatorialFrame",
                    Chain = new List<FrameChainLinkDto>
                    {
                        new()
                        {
                            BaseFrame = new() { Type = "mantle" },
                            Transform = new() { W = 0.707, X = 0.0, Y = 0.0, Z = 0.707 },
                            ValidityRange = null,
                            SequenceHint = null
                        }
                    },
                    Metadata = null
                }
            },
            Magnitude = 1.0,
            Azimuth = 0.0
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.RelativeToFrame.Type.Should().Be("custom");
        var def = roundtripDto.RelativeToFrame.Definition.Should().NotBeNull().Subject;
        def.Name.Should().Be("EquatorialFrame");
        def.Chain.Should().HaveCount(1);
        def.Chain[0].BaseFrame.Should().NotBeNull();
        def.Chain[0].Transform.W.Should().BeApproximately(0.707, 0.001);
        def.Chain[0].Transform.Z.Should().BeApproximately(0.707, 0.001);
        def.Metadata.Should().BeNull();
    }

    #endregion

    #region MessagePack Roundtrip Tests

    [Fact]
    public void MessagePackRoundtrip_MantleFrame_preservesAllValues()
    {
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(1.5, -2.5, 0.75),
            DeformationComponent = null,
            RelativeToFrame = MantleFrame.Instance,
            Magnitude = 3.041,
            Azimuth = -1.030
        };

        var serialized = MessagePackSerializer.Serialize(domain);
        var deserialized = MessagePackSerializer.Deserialize<VelocityDecomposition>(serialized);

        deserialized.Should().BeEquivalentTo(domain);
    }

    [Fact]
    public void MessagePackRoundtrip_PlateAnchorFrame_preservesAllValues()
    {
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(0.5, -0.3, 0.1),
            DeformationComponent = new(0.01, 0.02, 0.0),
            RelativeToFrame = new PlateAnchor { PlateId = plateId },
            Magnitude = 0.583,
            Azimuth = -0.540
        };

        var serialized = MessagePackSerializer.Serialize(domain);
        var deserialized = MessagePackSerializer.Deserialize<VelocityDecomposition>(serialized);

        deserialized.RigidRotationComponent.X.Should().Be(0.5);
        deserialized.RigidRotationComponent.Y.Should().Be(-0.3);
        deserialized.RigidRotationComponent.Z.Should().Be(0.1);
        var deformation = deserialized.DeformationComponent.Should().NotBeNull().Subject;
        deformation.X.Should().Be(0.01);
        deformation.Y.Should().Be(0.02);
        deformation.Z.Should().Be(0.0);
        deserialized.RelativeToFrame.Should().BeOfType<PlateAnchor>();
        ((PlateAnchor)deserialized.RelativeToFrame).PlateId.Value.Should().Be(plateId.Value);
        deserialized.Magnitude.Should().Be(0.583);
        deserialized.Azimuth.Should().Be(-0.540);
    }

    [Fact]
    public void MessagePackRoundtrip_CustomFrame_preservesAllValues()
    {
        var domain = new VelocityDecomposition
        {
            RigidRotationComponent = new(1.0, 0.0, 0.0),
            DeformationComponent = null,
            RelativeToFrame = new CustomFrame
            {
                Definition = new()
                {
                    Name = "TestCustomFrame",
                    Chain = new List<FrameChainLink>
                    {
                        new()
                        {
                            BaseFrame = MantleFrame.Instance,
                            Transform = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.5),
                            ValidityRange = new() { StartTick = new(0), EndTick = new(100) },
                            SequenceHint = 42
                        }
                    },
                    Metadata = new() { Description = "A custom test frame", Author = "MapperTests" }
                }
            },
            Magnitude = 1.0,
            Azimuth = 0.0
        };

        var serialized = MessagePackSerializer.Serialize(domain);
        var deserialized = MessagePackSerializer.Deserialize<VelocityDecomposition>(serialized);

        var custom = deserialized.RelativeToFrame.Should().BeOfType<CustomFrame>().Subject;
        custom.Definition.Name.Should().Be("TestCustomFrame");
        custom.Definition.Chain.Should().HaveCount(1);
        custom.Definition.Chain[0].BaseFrame.Should().BeOfType<MantleFrame>();
        custom.Definition.Chain[0].ValidityRange.Should().NotBeNull();
        custom.Definition.Chain[0].ValidityRange!.Value.StartTick.Value.Should().Be(0);
        custom.Definition.Chain[0].ValidityRange!.Value.EndTick.Value.Should().Be(100);
        custom.Definition.Chain[0].SequenceHint.Should().Be(42);
        custom.Definition.Metadata.Should().NotBeNull();
        custom.Definition.Metadata!.Description.Should().Be("A custom test frame");
        custom.Definition.Metadata!.Author.Should().Be("MapperTests");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ToDomain_UnknownFrameType_ThrowsArgumentException()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.0, Y = 0.0, Z = 0.0 },
            RelativeToFrame = new() { Type = "unknownFrameType" }
        };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_PlateAnchorWithNullPlateId_ThrowsArgumentException()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.0, Y = 0.0, Z = 0.0 },
            RelativeToFrame = new() { Type = "plateAnchor", PlateId = null }
        };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_CustomWithNullDefinition_ThrowsArgumentException()
    {
        var dto = new VelocityDecompositionDto
        {
            RigidRotationComponent = new() { X = 0.0, Y = 0.0, Z = 0.0 },
            RelativeToFrame = new() { Type = "custom", Definition = null }
        };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    #endregion
}
