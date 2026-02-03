# RFC-V2-0045 Reconstruction Query Contract - Audit & Implementation Plan

**Date**: 2026-02-03
**Status**: Partially Implemented
**Worktree**: `../worktrees/fantasim-world--rfc-v2-0045-reconstruction-query-contract`

---

## Audit Summary

### ✅ IMPLEMENTED
- `ReconstructionPolicy` - Complete policy abstraction
- `IPlateReconstructionSolver` - Boundary reconstruction interface
- `IPlateFeatureReconstructionSolver` - Feature reconstruction interface
- `NaivePlateReconstructionSolver` - Basic implementation
- `IFeaturePlateAssigner` - Plate assignment interface
- `ReconstructedFeature` / `ReconstructedBoundary` - Minimal output types
- `PolicyCacheKey` - Cache key with topology/kinematics hashes
- `IPlateVelocitySolver` - Raw velocity computation

### ❌ MISSING (RFC Non-Compliant)
1. **`ReconstructResult`** - Wrapper with Provenance + Metadata
2. **`QueryMetadata`** - Hashes, timing, versioning
3. **`PlateAssignmentResult`** - Confidence, candidates, distance
4. **`VelocityResult`** - With decomposition
5. **`VelocityDecomposition`** - Rigid, deformation, relative components
6. **`ProvenanceChain`** - Complete provenance structure
7. **Stable sorting** - By SourceFeatureId.Value
8. **Cursor-based pagination** - For large result sets
9. **Policy validation** - Per query type
10. **Test gates** - RFC §7 compliance

### ⚠️ PARTIAL GAPS
- `ReconstructedFeature` missing: `AnchorPlateId`, `ReconstructionAge`
- Solver uses legacy `ReconstructionOptions` instead of `ReconstructionPolicy`
- Velocity returns raw `Velocity3d` not `VelocityResult` with decomposition

---

## Implementation Tasks

### Phase 1: Core Data Structures
- [ ] Add `ProvenanceChain` record/class
- [ ] Add `QueryMetadata` record/class
- [ ] Add `ReconstructResult` wrapper
- [ ] Add `ReconstructedFeature` missing fields
- [ ] Add `PlateAssignmentResult` with confidence enum
- [ ] Add `VelocityResult` and `VelocityDecomposition`

### Phase 2: Update Contracts
- [ ] Update `IPlateReconstructionSolver` to use `ReconstructionPolicy`
- [ ] Update `IPlateFeatureReconstructionSolver` to use `ReconstructionPolicy`
- [ ] Update `NaivePlateReconstructionSolver` implementation
- [ ] Add `IPlateVelocityQuery` service interface
- [ ] Add `IPlateAssignmentQuery` service interface

### Phase 3: Provenance & Metadata
- [ ] Implement provenance injection in solvers
- [ ] Implement stream hash computation
- [ ] Implement metadata population

### Phase 4: Test Gates
- [ ] Add output determinism tests
- [ ] Add geometry hash stability tests
- [ ] Add pagination stability tests
- [ ] Add provenance completeness tests
- [ ] Add cache invalidation tests

---

## Files to Modify

### New Files
- `Geosphere.Plate.Reconstruction.Contracts/Provenance/ProvenanceChain.cs`
- `Geosphere.Plate.Reconstruction.Contracts/Output/ReconstructResult.cs`
- `Geosphere.Plate.Reconstruction.Contracts/Output/QueryMetadata.cs`
- `Geosphere.Plate.Reconstruction.Contracts/Output/PlateAssignmentResult.cs`
- `Geosphere.Plate.Reconstruction.Contracts/Output/VelocityResult.cs`
- `Geosphere.Plate.Reconstruction.Contracts/Output/VelocityDecomposition.cs`
- `Geosphere.Plate.Reconstruction.Contracts/IPlateReconstructionQueryService.cs`

### Modified Files
- `Geosphere.Plate.Reconstruction.Contracts/Output/ReconstructedFeature.cs`
- `Geosphere.Plate.Reconstruction.Contracts/IPlateReconstructionSolver.cs`
- `Geosphere.Plate.Reconstruction.Contracts/IPlateFeatureReconstructionSolver.cs`
- `Geosphere.Plate.Reconstruction.Contracts/ReconstructionOptions.cs` (deprecate)
- `Geosphere.Plate.Reconstruction.Solver/NaivePlateReconstructionSolver.cs`
