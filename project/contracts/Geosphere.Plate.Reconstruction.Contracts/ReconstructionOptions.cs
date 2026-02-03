using System;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

/// <summary>
/// Options for reconstruction queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deprecated:</b> This class is obsolete. Use <see cref="ReconstructionPolicy"/> instead,
/// which provides a more comprehensive and flexible approach to configuring reconstruction behavior.
/// </para>
/// </remarks>
[Obsolete("Use ReconstructionPolicy instead. This class will be removed in a future version.", DiagnosticId = "FANTASIM0001")]
public sealed class ReconstructionOptions
{
    public static readonly ReconstructionOptions Default = new();

    /// <summary>
    /// Gets the reference frame to reconstruct into.
    /// </summary>
    public ReferenceFrameId? Frame { get; init; }

}
