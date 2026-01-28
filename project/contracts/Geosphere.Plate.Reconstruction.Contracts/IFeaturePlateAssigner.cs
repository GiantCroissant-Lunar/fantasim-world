namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public interface IFeaturePlateAssigner
{
    IReadOnlyList<ReconstructableFeature> AssignPlateProvenance(
        IReadOnlyList<ReconstructableFeature> features,
        IReadOnlyList<PlatePartitionRegion> partition);
}
