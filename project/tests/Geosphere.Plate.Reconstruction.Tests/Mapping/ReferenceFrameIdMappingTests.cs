using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Generated;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Mapping;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests.Mapping;

public class ReferenceFrameIdMappingTests
{
    private readonly ReferenceFrameIdMapper _mapper = new();

    #region DTO → Domain Mapping Tests

    [Fact]
    public void ToDomain_MantleFrame_ReturnsSingletonInstance()
    {
        var dto = new ReferenceFrameIdDto { Type = "mantle" };

        var domain = _mapper.ToDomain(dto);

        domain.Should().BeOfType<MantleFrame>();
        domain.Should().Be(MantleFrame.Instance);
    }

    [Fact]
    public void ToDomain_PlateAnchorFrame_ReturnsCorrectPlateId()
    {
        var plateId = Guid.NewGuid();
        var dto = new ReferenceFrameIdDto
        {
            Type = "plateAnchor",
            PlateId = plateId.ToString()
        };

        var domain = _mapper.ToDomain(dto);

        var anchor = domain.Should().BeOfType<PlateAnchor>().Subject;
        anchor.PlateId.Value.Should().Be(plateId);
    }

    [Fact]
    public void ToDomain_AbsoluteFrame_ReturnsSingletonInstance()
    {
        var dto = new ReferenceFrameIdDto { Type = "absolute" };

        var domain = _mapper.ToDomain(dto);

        domain.Should().BeOfType<AbsoluteFrame>();
        domain.Should().Be(AbsoluteFrame.Instance);
    }

    [Fact]
    public void ToDomain_CustomFrame_ReturnsCorrectDefinition()
    {
        var dto = new ReferenceFrameIdDto
        {
            Type = "custom",
            Definition = new FrameDefinitionDto
            {
                Name = "TestFrame",
                Chain = new List<FrameChainLinkDto>
                {
                    new()
                    {
                        BaseFrame = new() { Type = "mantle" },
                        Transform = new() { W = 1.0, X = 0.0, Y = 0.0, Z = 0.0 }
                    }
                }
            }
        };

        var domain = _mapper.ToDomain(dto);

        var custom = domain.Should().BeOfType<CustomFrame>().Subject;
        custom.Definition.Name.Should().Be("TestFrame");
        custom.Definition.Chain.Should().HaveCount(1);
        custom.Definition.Chain[0].BaseFrame.Should().BeOfType<MantleFrame>();
    }

    [Fact]
    public void ToDomain_NestedCustomFrame_MapsCorrectly()
    {
        var dto = new ReferenceFrameIdDto
        {
            Type = "custom",
            Definition = new FrameDefinitionDto
            {
                Name = "NestedFrame",
                Chain = new List<FrameChainLinkDto>
                {
                    new()
                    {
                        BaseFrame = new() { Type = "plateAnchor", PlateId = "11111111-1111-1111-1111-111111111111" },
                        Transform = new() { W = 0.707, X = 0.0, Y = 0.0, Z = 0.707 }
                    },
                    new()
                    {
                        BaseFrame = new() { Type = "absolute" },
                        Transform = new() { W = 0.0, X = 1.0, Y = 0.0, Z = 0.0 }
                    }
                }
            }
        };

        var domain = _mapper.ToDomain(dto);

        var custom = domain.Should().BeOfType<CustomFrame>().Subject;
        custom.Definition.Name.Should().Be("NestedFrame");
        custom.Definition.Chain.Should().HaveCount(2);

        var firstLink = custom.Definition.Chain[0];
        var anchor = firstLink.BaseFrame.Should().BeOfType<PlateAnchor>().Subject;
        anchor.PlateId.Value.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        custom.Definition.Chain[1].BaseFrame.Should().BeOfType<AbsoluteFrame>();
    }

    #endregion

    #region Domain → DTO Mapping Tests

    [Fact]
    public void ToDto_MantleFrame_ReturnsCorrectDto()
    {
        var domain = MantleFrame.Instance;

        var dto = _mapper.ToDto(domain);

        dto.Type.Should().Be("mantle");
        dto.PlateId.Should().BeNull();
        dto.Definition.Should().BeNull();
    }

    [Fact]
    public void ToDto_PlateAnchorFrame_ReturnsCorrectDto()
    {
        var plateId = new PlateId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var domain = new PlateAnchor { PlateId = plateId };

        var dto = _mapper.ToDto(domain);

        dto.Type.Should().Be("plateAnchor");
        dto.PlateId.Should().Be("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        dto.Definition.Should().BeNull();
    }

    [Fact]
    public void ToDto_AbsoluteFrame_ReturnsCorrectDto()
    {
        var domain = AbsoluteFrame.Instance;

        var dto = _mapper.ToDto(domain);

        dto.Type.Should().Be("absolute");
        dto.PlateId.Should().BeNull();
        dto.Definition.Should().BeNull();
    }

    [Fact]
    public void ToDto_CustomFrame_ReturnsCorrectDto()
    {
        var domain = new CustomFrame
        {
            Definition = new()
            {
                Name = "TestFrame",
                Chain = new List<FrameChainLink>
                {
                    new()
                    {
                        BaseFrame = MantleFrame.Instance,
                        Transform = new FiniteRotation(new(1.0, 0.0, 0.0, 0.0))
                    }
                }
            }
        };

        var dto = _mapper.ToDto(domain);

        dto.Type.Should().Be("custom");
        dto.Definition.Should().NotBeNull();
        dto.Definition!.Name.Should().Be("TestFrame");
        dto.Definition!.Chain.Should().HaveCount(1);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_MantleFrame_PreservesAllValues()
    {
        var originalDto = new ReferenceFrameIdDto { Type = "mantle" };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.Type.Should().Be(originalDto.Type);
        roundtripDto.PlateId.Should().BeNull();
        roundtripDto.Definition.Should().BeNull();
    }

    [Fact]
    public void Roundtrip_PlateAnchorFrame_PreservesAllValues()
    {
        var plateId = Guid.NewGuid();
        var originalDto = new ReferenceFrameIdDto
        {
            Type = "plateAnchor",
            PlateId = plateId.ToString()
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.Type.Should().Be("plateAnchor");
        roundtripDto.PlateId.Should().Be(plateId.ToString());
        roundtripDto.Definition.Should().BeNull();
    }

    [Fact]
    public void Roundtrip_CustomFrameWithMetadata_PreservesAllValues()
    {
        var originalDto = new ReferenceFrameIdDto
        {
            Type = "custom",
            Definition = new FrameDefinitionDto
            {
                Name = "EquatorialFrame",
                Chain = new List<FrameChainLinkDto>
                {
                    new()
                    {
                        BaseFrame = new() { Type = "absolute" },
                        Transform = new() { W = 0.707, X = 0.0, Y = 0.0, Z = 0.707 },
                        SequenceHint = 1
                    }
                },
                Metadata = new() { Description = "Rotated to equator", Author = "TestSuite" }
            }
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.Type.Should().Be("custom");
        var def = roundtripDto.Definition.Should().NotBeNull().Subject;
        def.Name.Should().Be("EquatorialFrame");
        def.Chain.Should().HaveCount(1);
        def.Chain[0].BaseFrame.Should().NotBeNull();
        def.Chain[0].Transform.W.Should().BeApproximately(0.707, 0.001);
        def.Chain[0].Transform.Z.Should().BeApproximately(0.707, 0.001);
        def.Metadata.Should().NotBeNull();
        def.Metadata!.Description.Should().Be("Rotated to equator");
        def.Metadata!.Author.Should().Be("TestSuite");
    }

    [Fact]
    public void Roundtrip_CustomFrameWithValidityRange_PreservesAllValues()
    {
        var originalDto = new ReferenceFrameIdDto
        {
            Type = "custom",
            Definition = new FrameDefinitionDto
            {
                Name = "TemporalFrame",
                Chain = new List<FrameChainLinkDto>
                {
                    new()
                    {
                        BaseFrame = new() { Type = "plateAnchor", PlateId = "bbbbbbbb-cccc-dddd-eeee-ffffffffffff" },
                        Transform = new() { W = 0.0, X = 0.0, Y = 1.0, Z = 0.0 },
                        ValidityRange = new() { StartTick = 100, EndTick = 500 }
                    }
                }
            }
        };

        var domain = _mapper.ToDomain(originalDto);
        var roundtripDto = _mapper.ToDto(domain);

        roundtripDto.Type.Should().Be("custom");
        var def = roundtripDto.Definition.Should().NotBeNull().Subject;
        def.Chain[0].ValidityRange.Should().NotBeNull();
        def.Chain[0].ValidityRange!.StartTick.Should().Be(100);
        def.Chain[0].ValidityRange!.EndTick.Should().Be(500);
    }

    #endregion

    #region FrameDefinition Mapping Tests

    [Fact]
    public void ToDomain_FrameDefinitionDto_MapsAllProperties()
    {
        var dto = new FrameDefinitionDto
        {
            Name = "FullDefinition",
            Chain = new List<FrameChainLinkDto>
            {
                new() { BaseFrame = new() { Type = "mantle" }, Transform = new() { W = 1.0, X = 0.0, Y = 0.0, Z = 0.0 } }
            },
            Metadata = new() { Description = "Complete test", Author = "TestRunner" }
        };

        var domain = _mapper.ToDomain(dto);

        domain.Name.Should().Be("FullDefinition");
        domain.Chain.Should().HaveCount(1);
        domain.Metadata.Should().NotBeNull();
        domain.Metadata!.Description.Should().Be("Complete test");
        domain.Metadata!.Author.Should().Be("TestRunner");
    }

    [Fact]
    public void ToDto_FrameDefinitionDto_MapsAllProperties()
    {
        var domain = new FrameDefinition
        {
            Name = "FullDomain",
            Chain = new List<FrameChainLink>
            {
                new() { BaseFrame = MantleFrame.Instance, Transform = new FiniteRotation(new(0.0, 1.0, 0.0, 0.0)) }
            },
            Metadata = new() { Description = "Domain description", Author = "DomainMapper" }
        };

        var dto = _mapper.ToDto(domain);

        dto.Name.Should().Be("FullDomain");
        dto.Chain.Should().HaveCount(1);
        dto.Metadata.Should().NotBeNull();
        dto.Metadata!.Description.Should().Be("Domain description");
        dto.Metadata!.Author.Should().Be("DomainMapper");
    }

    #endregion

    #region FrameChainLink Mapping Tests

    [Fact]
    public void ToDomain_FrameChainLinkDto_MapsAllProperties()
    {
        var dto = new FrameChainLinkDto
        {
            BaseFrame = new() { Type = "absolute" },
            Transform = new() { W = 0.5, X = 0.5, Y = 0.5, Z = 0.5 },
            ValidityRange = new() { StartTick = 0, EndTick = 1000 },
            SequenceHint = 42
        };

        var domain = _mapper.ToDomain(dto);

        domain.BaseFrame.Should().BeOfType<AbsoluteFrame>();
        domain.Transform.Orientation.W.Should().Be(0.5);
        domain.Transform.Orientation.X.Should().Be(0.5);
        domain.Transform.Orientation.Y.Should().Be(0.5);
        domain.Transform.Orientation.Z.Should().Be(0.5);
        domain.ValidityRange.Should().NotBeNull();
        domain.ValidityRange!.Value.StartTick.Value.Should().Be(0);
        domain.ValidityRange!.Value.EndTick.Value.Should().Be(1000);
        domain.SequenceHint.Should().Be(42);
    }

    [Fact]
    public void ToDto_FrameChainLinkDto_MapsAllProperties()
    {
        var domain = new FrameChainLink
        {
            BaseFrame = AbsoluteFrame.Instance,
            Transform = new FiniteRotation(new(0.0, 0.0, 0.0, 1.0)),
            ValidityRange = new() { StartTick = new(500), EndTick = new(1500) },
            SequenceHint = 99
        };

        var dto = _mapper.ToDto(domain);

        dto.BaseFrame.Type.Should().Be("absolute");
        dto.Transform.W.Should().Be(0.0);
        dto.Transform.X.Should().Be(0.0);
        dto.Transform.Y.Should().Be(0.0);
        dto.Transform.Z.Should().Be(1.0);
        dto.ValidityRange.Should().NotBeNull();
        dto.ValidityRange!.StartTick.Should().Be(500);
        dto.ValidityRange!.EndTick.Should().Be(1500);
        dto.SequenceHint.Should().Be(99);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ToDomain_UnknownFrameType_ThrowsArgumentException()
    {
        var dto = new ReferenceFrameIdDto { Type = "unknownFrameType" };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_PlateAnchorWithNullPlateId_ThrowsArgumentException()
    {
        var dto = new ReferenceFrameIdDto { Type = "plateAnchor", PlateId = null };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_PlateAnchorWithEmptyPlateId_ThrowsArgumentException()
    {
        var dto = new ReferenceFrameIdDto { Type = "plateAnchor", PlateId = string.Empty };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_CustomWithNullDefinition_ThrowsArgumentException()
    {
        var dto = new ReferenceFrameIdDto { Type = "custom", Definition = null };

        Assert.Throws<ArgumentException>(() => _mapper.ToDomain(dto));
    }

    #endregion
}
