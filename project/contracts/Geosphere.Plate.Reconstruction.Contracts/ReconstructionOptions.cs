using FantaSim.Geosphere.Plate.Kinematics.Contracts;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public sealed class ReconstructionOptions
{
    public static readonly ReconstructionOptions Default = new();

    /// <summary>
    /// Gets the reference frame to reconstruct into.
    /// </summary>
    public ReferenceFrameId? Frame { get; init; }

}
