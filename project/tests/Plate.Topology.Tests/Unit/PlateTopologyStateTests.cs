using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Unit;

public sealed class PlateTopologyStateTests
{
    [Fact]
    public void NewState_HasExpectedEmptyShape()
    {
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var state = new PlateTopologyState(identity);

        Assert.Equal(identity, state.Identity);
        Assert.Equal(-1, state.LastEventSequence);
        Assert.Empty(state.Plates);
        Assert.Empty(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.Empty(state.Violations);
    }

    [Fact]
    public void PlateTopologyState_ImplementsIPlateTopologyStateView()
    {
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var state = new PlateTopologyState(identity);
        IPlateTopologyStateView view = state;

        Assert.NotNull(view);
        Assert.Equal(identity, view.Identity);
        Assert.Empty(view.Plates);
        Assert.Empty(view.Boundaries);
        Assert.Empty(view.Junctions);
        Assert.Equal(-1, view.LastEventSequence);
    }

    [Fact]
    public void SampleDerivedProductGenerator_CanAcceptPlateTopologyState()
    {
        var identity = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var state = new PlateTopologyState(identity);
        var generator = new SampleDerivedGenerator();

        var product = generator.Generate(state);

        Assert.Equal("SampleProduct", product);
    }

    private sealed class SampleDerivedGenerator : IDerivedProductGenerator<string>
    {
        public string Generate(IPlateTopologyStateView state)
        {
            return "SampleProduct";
        }
    }
}
