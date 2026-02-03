using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.TruePolarWander;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Service.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

/// <summary>
/// RFC-V2-0046 Section 5.1: Implementation of frame-aware kinematics view.
/// Combines IPlateKinematicsStateView with IFrameService to provide plate rotations
/// expressed in any reference frame.
/// </summary>
public sealed class FrameAwareKinematicsView : IFrameAwareKinematicsView
{
    private readonly IPlateKinematicsStateView _kinematics;
    private readonly IPlateTopologyStateView _topology;
    private readonly IFrameService _frameService;
    private readonly ModelId _modelId;
    private readonly ITruePolarWanderModel? _tpwModel;

    /// <summary>
    /// Creates a new instance of FrameAwareKinematicsView.
    /// </summary>
    /// <param name="kinematics">The underlying kinematics state view.</param>
    /// <param name="topology">The topology state view for plate enumeration.</param>
    /// <param name="frameService">The frame service for computing frame transforms.</param>
    /// <param name="modelId">The kinematics model ID.</param>
    /// <param name="tpwModel">Optional True Polar Wander model for absolute frame computations.</param>
    public FrameAwareKinematicsView(
        IPlateKinematicsStateView kinematics,
        IPlateTopologyStateView topology,
        IFrameService frameService,
        ModelId modelId,
        ITruePolarWanderModel? tpwModel = null)
    {
        _kinematics = kinematics ?? throw new ArgumentNullException(nameof(kinematics));
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        _frameService = frameService ?? throw new ArgumentNullException(nameof(frameService));
        _modelId = modelId;
        _tpwModel = tpwModel;
    }

    /// <inheritdoc />
    public FiniteRotation? GetRotationInFrame(PlateId plateId, CanonicalTick tick, ReferenceFrameId frame)
    {
        // Get the plate's base rotation in the mantle frame
        if (!_kinematics.TryGetRotation(plateId, tick, out var baseRotation))
        {
            return null;
        }

        // Get the transform from mantle frame to the target frame
        var frameTransform = _frameService.GetFrameTransform(
            MantleFrame.Instance,
            frame,
            tick,
            _modelId,
            _kinematics,
            _topology,
            _tpwModel);

        // If the frame transform is invalid, return null to avoid producing misleading results
        // The caller should handle this as an unavailable rotation rather than using potentially incorrect data
        if (frameTransform.Validity == TransformValidity.Invalid)
        {
            return null;
        }

        // Compose: plate rotation in target frame = (mantle -> frame) ∘ (plate rotation in mantle)
        // plate rotation in mantle is baseRotation
        // mantle -> frame is frameTransform.Transform
        // result = frameTransform ∘ baseRotation
        var plateRotationInMantle = new FiniteRotation(baseRotation);
        var rotationInFrame = plateRotationInMantle.Compose(frameTransform.Transform);

        return rotationInFrame;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<PlateId, FiniteRotation> GetAllRotationsInFrame(CanonicalTick tick, ReferenceFrameId frame)
    {
        // Get the transform from mantle frame to the target frame (once for all plates)
        var frameTransform = _frameService.GetFrameTransform(
            MantleFrame.Instance,
            frame,
            tick,
            _modelId,
            _kinematics,
            _topology,
            _tpwModel);

        // If the frame transform is invalid, return an empty dictionary to avoid producing misleading results
        if (frameTransform.Validity == TransformValidity.Invalid)
        {
            return new Dictionary<PlateId, FiniteRotation>();
        }

        var result = new Dictionary<PlateId, FiniteRotation>();

        // Iterate over all known plates from topology
        foreach (var (plateId, plate) in _topology.Plates)
        {
            // Skip retired plates
            if (plate.IsRetired)
            {
                continue;
            }

            // Get the plate's base rotation
            if (_kinematics.TryGetRotation(plateId, tick, out var baseRotation))
            {
                var plateRotationInMantle = new FiniteRotation(baseRotation);
                var rotationInFrame = plateRotationInMantle.Compose(frameTransform.Transform);
                result[plateId] = rotationInFrame;
            }
        }

        return result;
    }
}
