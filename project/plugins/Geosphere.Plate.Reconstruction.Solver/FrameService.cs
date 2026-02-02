using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Service.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

public sealed class FrameService : IFrameService
{
    public FrameTransformResult GetFrameTransform(
        ReferenceFrameId fromFrame,
        ReferenceFrameId toFrame,
        CanonicalTick tick,
        FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId modelId,
        IPlateKinematicsStateView kinematics,
        IPlateTopologyStateView topology)
    {
        var fromToMantle = GetFrameToMantleTransform(fromFrame, tick, modelId, kinematics);
        var toToMantle = GetFrameToMantleTransform(toFrame, tick, modelId, kinematics);

        // from -> mantle -> to = (from -> mantle) ∘ (mantle -> to)
        var transform = fromToMantle.Transform.Compose(toToMantle.Transform.Inverted());

        var provenance = new FrameTransformProvenance
        {
            FromFrame = fromFrame,
            ToFrame = toFrame,
            EvaluationChain = fromToMantle.Provenance.EvaluationChain.Concat(toToMantle.Provenance.EvaluationChain).ToArray(),
            KinematicsModelVersion = kinematics.Identity.ToString()
        };

        return new FrameTransformResult
        {
            Transform = transform,
            Provenance = provenance,
            Validity = TransformValidity.Valid
        };
    }

    private static FrameTransformResult GetFrameToMantleTransform(
        ReferenceFrameId frame,
        CanonicalTick tick,
        FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId modelId,
        IPlateKinematicsStateView kinematics)
    {
        switch (frame)
        {
            case MantleFrame:
                return new FrameTransformResult
                {
                    Transform = FiniteRotation.Identity,
                    Provenance = new FrameTransformProvenance
                    {
                        FromFrame = frame,
                        ToFrame = MantleFrame.Instance,
                        EvaluationChain = Array.Empty<FrameChainLink>(),
                        KinematicsModelVersion = kinematics.Identity.ToString()
                    },
                    Validity = TransformValidity.Valid
                };

            case PlateAnchor anchor:
                if (kinematics.TryGetRotation(anchor.PlateId, tick, out var rotationQ))
                {
                    return new FrameTransformResult
                    {
                        Transform = new FiniteRotation(rotationQ),
                        Provenance = new FrameTransformProvenance
                        {
                            FromFrame = frame,
                            ToFrame = MantleFrame.Instance,
                            EvaluationChain = Array.Empty<FrameChainLink>(),
                            KinematicsModelVersion = kinematics.Identity.ToString()
                        },
                        Validity = TransformValidity.Valid
                    };
                }

                return new FrameTransformResult
                {
                    Transform = FiniteRotation.Identity,
                    Provenance = new FrameTransformProvenance
                    {
                        FromFrame = frame,
                        ToFrame = MantleFrame.Instance,
                        EvaluationChain = Array.Empty<FrameChainLink>(),
                        KinematicsModelVersion = kinematics.Identity.ToString()
                    },
                    Validity = TransformValidity.Invalid
                };

            case AbsoluteFrame:
                return new FrameTransformResult
                {
                    Transform = FiniteRotation.Identity,
                    Provenance = new FrameTransformProvenance
                    {
                        FromFrame = frame,
                        ToFrame = MantleFrame.Instance,
                        EvaluationChain = Array.Empty<FrameChainLink>(),
                        KinematicsModelVersion = kinematics.Identity.ToString()
                    },
                    Validity = TransformValidity.Valid
                };

            case CustomFrame custom:
                return EvaluateCustomFrame(custom, tick, modelId, kinematics);

            default:
                throw new NotImplementedException($"Frame type {frame.GetType().Name} not supported");
        }
    }

    private static FrameTransformResult EvaluateCustomFrame(
        CustomFrame custom,
        CanonicalTick tick,
        FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId modelId,
        IPlateKinematicsStateView kinematics)
    {
        var activeLink = custom.Definition.Chain
            .Select((link, index) => (link, index))
            .Where(x => x.link.ValidityRange is null || x.link.ValidityRange.Value.Contains(tick))
            .OrderBy(x => x.link.SequenceHint ?? int.MaxValue)
            .ThenBy(x => x.index)
            .Select(x => x.link)
            .FirstOrDefault();

        if (activeLink is null)
        {
            return new FrameTransformResult
            {
                Transform = FiniteRotation.Identity,
                Provenance = new FrameTransformProvenance
                {
                    FromFrame = custom,
                    ToFrame = MantleFrame.Instance,
                    EvaluationChain = Array.Empty<FrameChainLink>(),
                    KinematicsModelVersion = kinematics.Identity.ToString()
                },
                Validity = TransformValidity.Invalid
            };
        }

        var baseToMantle = GetFrameToMantleTransform(activeLink.BaseFrame, tick, modelId, kinematics);

        // base -> custom is activeLink.Transform, therefore custom -> base is inverse.
        // custom -> mantle = (custom -> base) ∘ (base -> mantle)
        var customToMantle = activeLink.Transform.Inverted().Compose(baseToMantle.Transform);

        var evaluationChain = baseToMantle.Provenance.EvaluationChain
            .Concat(new[] { activeLink })
            .ToArray();

        return new FrameTransformResult
        {
            Transform = customToMantle,
            Provenance = new FrameTransformProvenance
            {
                FromFrame = custom,
                ToFrame = MantleFrame.Instance,
                EvaluationChain = evaluationChain,
                KinematicsModelVersion = kinematics.Identity.ToString()
            },
            Validity = baseToMantle.Validity
        };
    }

    public void ValidateFrameDefinition(FrameDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new ArgumentException("Custom frame definition must have a non-empty Name.", nameof(definition));

        if (definition.Chain is null || definition.Chain.Count == 0)
            throw new ArgumentException("Custom frame definition must have at least one chain link.", nameof(definition));

        ValidateFrameAcyclicity(definition);
    }

    private static void ValidateFrameAcyclicity(FrameDefinition definition)
    {
        var visiting = new HashSet<ReferenceFrameId>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<ReferenceFrameId>(ReferenceEqualityComparer.Instance);

        void VisitFrame(ReferenceFrameId frame)
        {
            if (frame is not CustomFrame custom)
                return;

            if (visited.Contains(frame))
                return;

            if (!visiting.Add(frame))
                throw new CyclicFrameReferenceException($"Custom frame chain contains a cycle at '{custom.Definition.Name}'.");

            foreach (var link in custom.Definition.Chain)
            {
                VisitFrame(link.BaseFrame);
            }

            visiting.Remove(frame);
            visited.Add(frame);
        }

        foreach (var link in definition.Chain)
        {
            VisitFrame(link.BaseFrame);
        }
    }
}
