using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts.Tests;

public class SamplingDomainTests
{
    [Fact]
    public void GlobalFactory_CreatesCorrectDomain()
    {
        var domain = SamplingDomain.Global(5.0, GridRegistration.Pixel);

        domain.DomainType.Should().Be(SamplingDomainType.Regular);
        domain.Extent.Should().NotBeNull();
        domain.Extent!.MinLat.Should().Be(-90);
        domain.Extent!.MaxLat.Should().Be(90);
        domain.Grid.Should().NotBeNull();
        domain.Grid!.ResolutionDeg.Should().Be(5.0);
        domain.DomainId.Should().StartWith("global-5.000000-pixel");
    }

    [Fact]
    public void DomainId_IsDeterministicForSameInput()
    {
        var d1 = SamplingDomain.Global(1.0);
        var d2 = SamplingDomain.Global(1.0);

        d1.DomainId.Should().Be(d2.DomainId);
    }
}
