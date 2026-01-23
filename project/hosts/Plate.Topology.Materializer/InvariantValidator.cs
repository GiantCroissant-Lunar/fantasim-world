using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using System.Linq;

namespace Plate.Topology.Materializer;

/// <summary>
/// Validates topology invariants for plate topology state per FR-010, FR-016.
///
/// The validator enforces critical topological constraints to ensure the
/// materialized state is valid and consistent. Violations are surfaced as
/// InvalidOperationException with clear messages indicating the invariant,
/// sequence number, and affected entities.
///
/// Invariants enforced:
/// 1. Boundary separates two plates: PlateIdLeft and PlateIdRight must exist in state
/// 2. No orphan boundaries/junctions: junctions must only reference existing non-retired boundaries
/// 3. Lifecycle ordering: no mutation after retirement
/// 4. Reference validity: events referencing missing/retired entities are rejected
/// 5. FR-016 boundary deletion: junctions referencing retired boundaries must be resolved via explicit events
/// </summary>
public static class InvariantValidator
{
    /// <summary>
    /// Validates all invariants for the materialized state.
    ///
    /// Throws InvalidOperationException if any invariant is violated, with a clear
    /// message indicating the invariant, sequence number, and affected entities.
    /// </summary>
    /// <param name="state">The materialized topology state to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any invariant is violated.
    /// </exception>
    public static void Validate(PlateTopologyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var violations = new List<InvariantViolation>();

        // Validate boundary plate references
        ValidateBoundaryPlateReferences(state, violations);

        // Validate junction boundary references (no orphan junctions)
        ValidateJunctionBoundaryReferences(state, violations);

        // Check for any violations and throw if found
        if (violations.Count > 0)
        {
            throw CreateViolationException(violations);
        }
    }

    /// <summary>
    /// Validates invariants during event application (pre-materialization validation).
    ///
    /// This is called during event application to validate event-specific invariants
    /// such as reference validity and lifecycle ordering.
    /// </summary>
    /// <param name="state">The current materialized state.</param>
    /// <param name="evt">The event being applied.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the event violates invariants.
    /// </exception>
    public static void ValidateEvent(PlateTopologyState state, IPlateTopologyEvent evt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(evt);

        switch (evt)
        {
            case BoundaryCreatedEvent boundaryCreated:
                ValidateBoundaryCreated(state, boundaryCreated);
                break;

            case BoundaryTypeChangedEvent boundaryTypeChanged:
                ValidateBoundaryTypeChanged(state, boundaryTypeChanged);
                break;

            case BoundaryGeometryUpdatedEvent boundaryGeometryUpdated:
                ValidateBoundaryGeometryUpdated(state, boundaryGeometryUpdated);
                break;

            case BoundaryRetiredEvent boundaryRetired:
                ValidateBoundaryRetired(state, boundaryRetired);
                break;

            case JunctionCreatedEvent junctionCreated:
                ValidateJunctionCreated(state, junctionCreated);
                break;

            case JunctionUpdatedEvent junctionUpdated:
                ValidateJunctionUpdated(state, junctionUpdated);
                break;

            case JunctionRetiredEvent junctionRetired:
                ValidateJunctionRetired(state, junctionRetired);
                break;

            case PlateRetiredEvent plateRetired:
                ValidatePlateRetired(state, plateRetired);
                break;
        }
    }

    private static void ValidateBoundaryPlateReferences(PlateTopologyState state, List<InvariantViolation> violations)
    {
        // Invariant 1: boundary must separate two plates
        // Both PlateIdLeft and PlateIdRight must exist in state and not be retired
        foreach (var (_, boundary) in state.Boundaries)
        {
            if (boundary.IsRetired)
                continue; // Retired boundaries don't need to satisfy this invariant

            if (boundary.PlateIdLeft == boundary.PlateIdRight)
            {
                violations.Add(new InvariantViolation(
                    "BoundarySeparatesTwoPlates",
                    $"Boundary {boundary.BoundaryId} has identical left and right plate {boundary.PlateIdLeft}",
                    state.LastEventSequence
                ));
            }

            // Check if left plate exists
            if (!state.Plates.ContainsKey(boundary.PlateIdLeft))
            {
                violations.Add(new InvariantViolation(
                    "BoundarySeparatesTwoPlates",
                    $"Boundary {boundary.BoundaryId} references non-existent left plate {boundary.PlateIdLeft}",
                    state.LastEventSequence
                ));
            }
            else if (state.Plates[boundary.PlateIdLeft].IsRetired)
            {
                violations.Add(new InvariantViolation(
                    "BoundarySeparatesTwoPlates",
                    $"Boundary {boundary.BoundaryId} references retired left plate {boundary.PlateIdLeft}",
                    state.LastEventSequence
                ));
            }

            // Check if right plate exists
            if (!state.Plates.ContainsKey(boundary.PlateIdRight))
            {
                violations.Add(new InvariantViolation(
                    "BoundarySeparatesTwoPlates",
                    $"Boundary {boundary.BoundaryId} references non-existent right plate {boundary.PlateIdRight}",
                    state.LastEventSequence
                ));
            }
            else if (state.Plates[boundary.PlateIdRight].IsRetired)
            {
                violations.Add(new InvariantViolation(
                    "BoundarySeparatesTwoPlates",
                    $"Boundary {boundary.BoundaryId} references retired right plate {boundary.PlateIdRight}",
                    state.LastEventSequence
                ));
            }
        }
    }

    private static void ValidateJunctionBoundaryReferences(PlateTopologyState state, List<InvariantViolation> violations)
    {
        // Invariant 2: no orphan boundaries/junctions
        // Junctions must only reference existing non-retired boundaries
        // Invariant 5: FR-016 - junctions referencing retired boundaries must be resolved
        foreach (var (_, junction) in state.Junctions)
        {
            if (junction.IsRetired)
                continue; // Retired junctions don't need to satisfy this invariant

            foreach (var boundaryId in junction.BoundaryIds)
            {
                // Check if boundary exists
                if (!state.Boundaries.ContainsKey(boundaryId))
                {
                    violations.Add(new InvariantViolation(
                        "NoOrphanJunctions",
                        $"Junction {junction.JunctionId} references non-existent boundary {boundaryId}",
                        state.LastEventSequence
                    ));
                }
                // Check if boundary is retired (FR-016 violation)
                else if (state.Boundaries[boundaryId].IsRetired)
                {
                    violations.Add(new InvariantViolation(
                        "NoOrphanJunctions",
                        $"Junction {junction.JunctionId} references retired boundary {boundaryId} (FR-016: must update or retire junction via explicit event)",
                        state.LastEventSequence
                    ));
                }
            }
        }
    }

    private static void ValidateBoundaryCreated(PlateTopologyState state, BoundaryCreatedEvent evt)
    {
        if (evt.PlateIdLeft == evt.PlateIdRight)
        {
            throw new InvalidOperationException(
                $"Invariant violation: BoundarySeparatesTwoPlates " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} creation has identical left and right plate {evt.PlateIdLeft}");
        }

        // Invariant 1: boundary must separate two existing plates
        if (!state.Plates.ContainsKey(evt.PlateIdLeft))
        {
            throw new InvalidOperationException(
                $"Invariant violation: BoundarySeparatesTwoPlates " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} creation references non-existent left plate {evt.PlateIdLeft}");
        }

        if (!state.Plates.ContainsKey(evt.PlateIdRight))
        {
            throw new InvalidOperationException(
                $"Invariant violation: BoundarySeparatesTwoPlates " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} creation references non-existent right plate {evt.PlateIdRight}");
        }

        // Check if referenced plates are retired
        if (state.Plates[evt.PlateIdLeft].IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: BoundarySeparatesTwoPlates " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} creation references retired left plate {evt.PlateIdLeft}");
        }

        if (state.Plates[evt.PlateIdRight].IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: BoundarySeparatesTwoPlates " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} creation references retired right plate {evt.PlateIdRight}");
        }
    }

    private static void ValidateBoundaryTypeChanged(PlateTopologyState state, BoundaryTypeChangedEvent evt)
    {
        // Invariant 3: lifecycle ordering - no mutation after retirement
        // Invariant 4: reference validity - boundary must exist and not be retired
        if (!state.Boundaries.ContainsKey(evt.BoundaryId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary type change references non-existent boundary {evt.BoundaryId}");
        }

        var boundary = state.Boundaries[evt.BoundaryId];
        if (boundary.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Cannot change type of retired boundary {evt.BoundaryId} (no mutation after retirement)");
        }
    }

    private static void ValidateBoundaryGeometryUpdated(PlateTopologyState state, BoundaryGeometryUpdatedEvent evt)
    {
        // Invariant 3: lifecycle ordering - no mutation after retirement
        // Invariant 4: reference validity - boundary must exist and not be retired
        if (!state.Boundaries.ContainsKey(evt.BoundaryId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary geometry update references non-existent boundary {evt.BoundaryId}");
        }

        var boundary = state.Boundaries[evt.BoundaryId];
        if (boundary.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Cannot update geometry of retired boundary {evt.BoundaryId} (no mutation after retirement)");
        }
    }

    private static void ValidateBoundaryRetired(PlateTopologyState state, BoundaryRetiredEvent evt)
    {
        // Invariant 4: reference validity - boundary must exist
        if (!state.Boundaries.ContainsKey(evt.BoundaryId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary retirement references non-existent boundary {evt.BoundaryId}");
        }

        var boundary = state.Boundaries[evt.BoundaryId];
        if (boundary.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Boundary {evt.BoundaryId} is already retired");
        }

        // FR-016: Check if any active junctions reference this boundary
        var activeJunctionsReferencingBoundary = state.Junctions
            .Where(kvp => !kvp.Value.IsRetired && kvp.Value.BoundaryIds.Contains(evt.BoundaryId))
            .Select(kvp => kvp.Key)
            .ToArray();

        if (activeJunctionsReferencingBoundary.Length > 0)
        {
            throw new InvalidOperationException(
                $"Invariant violation: FR-016 BoundaryDeletion " +
                $"[Sequence {evt.Sequence}] " +
                $"Cannot retire boundary {evt.BoundaryId} while {activeJunctionsReferencingBoundary.Length} active junction(s) reference it: " +
                $"{string.Join(", ", activeJunctionsReferencingBoundary)}. " +
                $"Must update or retire these junctions via explicit JunctionUpdated/JunctionRetired events first.");
        }
    }

    private static void ValidateJunctionCreated(PlateTopologyState state, JunctionCreatedEvent evt)
    {
        // Invariant 2: no orphan junctions - all referenced boundaries must exist and be non-retired
        foreach (var boundaryId in evt.BoundaryIds)
        {
            if (!state.Boundaries.ContainsKey(boundaryId))
            {
                throw new InvalidOperationException(
                    $"Invariant violation: NoOrphanJunctions " +
                    $"[Sequence {evt.Sequence}] " +
                    $"Junction {evt.JunctionId} creation references non-existent boundary {boundaryId}");
            }

            if (state.Boundaries[boundaryId].IsRetired)
            {
                throw new InvalidOperationException(
                    $"Invariant violation: NoOrphanJunctions " +
                    $"[Sequence {evt.Sequence}] " +
                    $"Junction {evt.JunctionId} creation references retired boundary {boundaryId}");
            }
        }
    }

    private static void ValidateJunctionUpdated(PlateTopologyState state, JunctionUpdatedEvent evt)
    {
        // Invariant 3: lifecycle ordering - no mutation after retirement
        // Invariant 4: reference validity - junction must exist and not be retired
        if (!state.Junctions.ContainsKey(evt.JunctionId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Junction update references non-existent junction {evt.JunctionId}");
        }

        var junction = state.Junctions[evt.JunctionId];
        if (junction.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Cannot update retired junction {evt.JunctionId} (no mutation after retirement)");
        }

        // Validate new boundary references if provided
        if (evt.NewBoundaryIds is not null)
        {
            foreach (var boundaryId in evt.NewBoundaryIds)
            {
                if (!state.Boundaries.ContainsKey(boundaryId))
                {
                    throw new InvalidOperationException(
                        $"Invariant violation: NoOrphanJunctions " +
                        $"[Sequence {evt.Sequence}] " +
                        $"Junction {evt.JunctionId} update references non-existent boundary {boundaryId}");
                }

                if (state.Boundaries[boundaryId].IsRetired)
                {
                    throw new InvalidOperationException(
                        $"Invariant violation: NoOrphanJunctions " +
                        $"[Sequence {evt.Sequence}] " +
                        $"Junction {evt.JunctionId} update references retired boundary {boundaryId}");
                }
            }
        }
    }

    private static void ValidateJunctionRetired(PlateTopologyState state, JunctionRetiredEvent evt)
    {
        // Invariant 4: reference validity - junction must exist
        if (!state.Junctions.ContainsKey(evt.JunctionId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Junction retirement references non-existent junction {evt.JunctionId}");
        }

        var junction = state.Junctions[evt.JunctionId];
        if (junction.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Junction {evt.JunctionId} is already retired");
        }
    }

    private static void ValidatePlateRetired(PlateTopologyState state, PlateRetiredEvent evt)
    {
        // Invariant 4: reference validity - plate must exist
        if (!state.Plates.ContainsKey(evt.PlateId))
        {
            throw new InvalidOperationException(
                $"Invariant violation: ReferenceValidity " +
                $"[Sequence {evt.Sequence}] " +
                $"Plate retirement references non-existent plate {evt.PlateId}");
        }

        var plate = state.Plates[evt.PlateId];
        if (plate.IsRetired)
        {
            throw new InvalidOperationException(
                $"Invariant violation: LifecycleOrdering " +
                $"[Sequence {evt.Sequence}] " +
                $"Plate {evt.PlateId} is already retired");
        }
    }

    private static InvalidOperationException CreateViolationException(List<InvariantViolation> violations)
    {
        var message = $"Invariant violation(s) detected [{violations.Count} total]:\n";

        for (int i = 0; i < violations.Count; i++)
        {
            var violation = violations[i];
            message += $"  {i + 1}. [{violation.Invariant}]";
            if (violation.Sequence.HasValue)
            {
                message += $" [Sequence {violation.Sequence.Value}]";
            }
            message += $": {violation.Message}\n";
        }

        return new InvalidOperationException(message);
    }
}
