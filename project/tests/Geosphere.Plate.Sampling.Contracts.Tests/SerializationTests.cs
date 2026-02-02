using FluentAssertions;
using MessagePack;
using UnifyGeometry;
using Xunit;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts.Tests;

public class SerializationTests
{
    [Fact]
    public void SamplingDomain_RoundTrips_MessagePack()
    {
        var original = SamplingDomain.Global(2.0);

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<SamplingDomain>(bytes);

        restored.Should().BeEquivalentTo(original);
        restored.DomainId.Should().Be(original.DomainId);
        restored.Grid!.NodeCount.Should().Be(original.Grid.NodeCount);
    }

    [Fact]
    public void GridSpec_RoundTrips_MessagePack()
    {
        var original = GridSpec.Global(1.0, GridRegistration.Gridline);

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<GridSpec>(bytes);

        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void LatLonExtent_RoundTrips_MessagePack()
    {
        var original = new LatLonExtent { MinLat = -10, MaxLat = 10, MinLon = 20, MaxLon = 30 };

        var bytes = MessagePackSerializer.Serialize(original);
        var restored = MessagePackSerializer.Deserialize<LatLonExtent>(bytes);

        restored.Should().BeEquivalentTo(original);
    }
}
