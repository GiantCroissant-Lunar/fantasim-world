using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

/// <summary>
/// RFC-V2-0046 Section 6.1: Frame chain canonicalization.
/// Simplifies and validates frame chains by removing redundant identity transforms,
/// merging consecutive constant transforms, and validating temporal consistency.
/// </summary>
public static class FrameChainCanonicalizer
{
    /// <summary>
    /// Canonicalizes a frame definition by simplifying its chain.
    /// Per RFC-V2-0046 Section 6.1:
    /// 1. Remove redundant identity transforms
    /// 2. Merge consecutive constant transforms where possible
    /// 3. Validate temporal consistency
    /// </summary>
    /// <param name="definition">The frame definition to canonicalize.</param>
    /// <returns>A new FrameDefinition with a simplified chain.</returns>
    /// <exception cref="ArgumentNullException">If definition is null.</exception>
    /// <exception cref="TemporalInconsistencyException">If the chain has overlapping or invalid ranges.</exception>
    public static FrameDefinition CanonicalizeFrameChain(FrameDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        // Handle edge case: empty chain
        if (definition.Chain is null || definition.Chain.Count == 0)
        {
            return definition with { Chain = Array.Empty<FrameChainLink>() };
        }

        // Step 1: Remove redundant identity transforms
        var simplified = definition.Chain
            .Where(link => !IsIdentity(link.Transform))
            .ToList();

        // Step 2: Merge consecutive constant transforms where possible
        var merged = MergeConsecutiveTransforms(simplified);

        // Step 3: Validate temporal consistency
        ValidateTemporalConsistency(merged);

        return definition with { Chain = merged };
    }

    /// <summary>
    /// Merges consecutive constant transforms in the chain where possible.
    /// Links can be merged when they have the same BaseFrame and adjacent/overlapping validity ranges.
    /// </summary>
    /// <param name="links">The chain links to merge.</param>
    /// <returns>A new list with consecutive transforms merged where possible.</returns>
    public static IReadOnlyList<FrameChainLink> MergeConsecutiveTransforms(IReadOnlyList<FrameChainLink> links)
    {
        ArgumentNullException.ThrowIfNull(links);

        if (links.Count <= 1)
        {
            return links;
        }

        // Sort links by validity range start tick (null ranges go first as they're "always valid")
        var sorted = links
            .OrderBy(link => link.SequenceHint ?? int.MaxValue)
            .ThenBy(link => link.ValidityRange?.StartTick.Value ?? long.MinValue)
            .ToList();

        var result = new List<FrameChainLink>();
        var current = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];

            // Check if we can merge current and next
            if (CanMerge(current, next))
            {
                current = MergeLinks(current, next);
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }

    /// <summary>
    /// Validates temporal consistency of the frame chain.
    /// Ensures there are no overlapping validity ranges for links with the same base frame.
    /// </summary>
    /// <param name="links">The chain links to validate.</param>
    /// <exception cref="TemporalInconsistencyException">If the chain has overlapping ranges.</exception>
    public static void ValidateTemporalConsistency(IReadOnlyList<FrameChainLink> links)
    {
        ArgumentNullException.ThrowIfNull(links);

        if (links.Count <= 1)
        {
            return;
        }

        // Group links by their base frame and check for overlaps within each group
        var groupedByBaseFrame = links
            .Where(link => link.ValidityRange is not null)
            .GroupBy(link => link.BaseFrame.ToString());

        foreach (var group in groupedByBaseFrame)
        {
            var sortedRanges = group
                .OrderBy(link => link.ValidityRange!.Value.StartTick.Value)
                .ToList();

            for (var i = 0; i < sortedRanges.Count - 1; i++)
            {
                var currentRange = sortedRanges[i].ValidityRange!.Value;
                var nextRange = sortedRanges[i + 1].ValidityRange!.Value;

                // Check for overlap: current end >= next start means overlap
                if (currentRange.EndTick.Value >= nextRange.StartTick.Value)
                {
                    throw new TemporalInconsistencyException(
                        $"Frame chain has overlapping validity ranges: " +
                        $"[{currentRange.StartTick.Value}, {currentRange.EndTick.Value}] overlaps with " +
                        $"[{nextRange.StartTick.Value}, {nextRange.EndTick.Value}] for base frame '{group.Key}'.");
                }
            }
        }
    }

    /// <summary>
    /// Determines if a rotation is an identity transform (within tolerance).
    /// </summary>
    /// <param name="rotation">The rotation to check.</param>
    /// <returns>True if the rotation is identity; otherwise false.</returns>
    public static bool IsIdentity(FiniteRotation rotation)
    {
        return rotation.IsIdentity;
    }

    /// <summary>
    /// Determines if two links can be merged.
    /// Links can be merged when:
    /// - They have the same BaseFrame
    /// - Both have no validity range (constants without time bounds), OR
    /// - Their validity ranges are adjacent (current end + 1 = next start)
    /// </summary>
    private static bool CanMerge(FrameChainLink current, FrameChainLink next)
    {
        // Must have the same base frame
        if (current.BaseFrame.ToString() != next.BaseFrame.ToString())
        {
            return false;
        }

        // Both null ranges (always valid) - can merge constants
        if (current.ValidityRange is null && next.ValidityRange is null)
        {
            return true;
        }

        // One has range, one doesn't - cannot merge
        if (current.ValidityRange is null || next.ValidityRange is null)
        {
            return false;
        }

        // Check if ranges are adjacent (current end + 1 = next start)
        var currentEnd = current.ValidityRange.Value.EndTick.Value;
        var nextStart = next.ValidityRange.Value.StartTick.Value;

        return currentEnd + 1 == nextStart;
    }

    /// <summary>
    /// Merges two links into one by composing their transforms.
    /// </summary>
    private static FrameChainLink MergeLinks(FrameChainLink first, FrameChainLink second)
    {
        // Compose transforms: first then second
        var composedTransform = first.Transform.Compose(second.Transform);

        // Compute merged validity range
        CanonicalTickRange? mergedRange = null;

        if (first.ValidityRange is not null && second.ValidityRange is not null)
        {
            mergedRange = new CanonicalTickRange
            {
                StartTick = first.ValidityRange.Value.StartTick,
                EndTick = second.ValidityRange.Value.EndTick
            };
        }

        return new FrameChainLink
        {
            BaseFrame = first.BaseFrame,
            Transform = composedTransform,
            ValidityRange = mergedRange,
            SequenceHint = first.SequenceHint ?? second.SequenceHint
        };
    }
}

/// <summary>
/// Thrown when a frame chain has temporal inconsistencies such as overlapping validity ranges.
/// </summary>
public sealed class TemporalInconsistencyException : InvalidOperationException
{
    public TemporalInconsistencyException(string message) : base(message)
    {
    }
}
