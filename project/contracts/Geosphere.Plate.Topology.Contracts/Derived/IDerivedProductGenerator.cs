namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

public interface IDerivedProductGenerator<TProduct>
{
    TProduct Generate(IPlateTopologyStateView state);
}
