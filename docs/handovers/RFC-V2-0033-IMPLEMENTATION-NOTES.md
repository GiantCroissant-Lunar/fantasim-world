---
title: "RFC-V2-0033 Implementation Notes & Status Update"
id: "rfc-v2-0033-implementation-notes"
description: "Implementation completion documentation and fantasim-hub RFC status update requirements"
date: "2026-01-28"
tags: ["rfc", "velocity", "solver", "handovers"]
---

**Date**: 2026-01-28
**Topic**: RFC-V2-0033 Plate Velocity Solver Implementation
**Repository**: fantasim-world (implementation) → fantasim-hub (RFC status update required)

---

## 1. RFC-V2-0033 Implementation Summary

RFC-V2-0033 specifies the **Plate Velocity Solver** interface and contracts for computing plate velocities from kinematics truth. The implementation is **complete** in the `fantasim-world` repository.

### Scope Implemented

| Component | Status | Notes |
|-----------|--------|-------|
| `IPlateVelocitySolver` interface | ✅ Complete | Defines absolute, relative, and angular velocity queries |
| `Velocity3d` struct | ✅ Complete | Double-precision 3D velocity vector with MessagePack serialization |
| `AngularVelocity3d` struct | ✅ Complete | Axis-angle representation with cross-product helper |
| `FiniteRotationPlateVelocitySolver` | ✅ Complete | Reference implementation using finite difference method |
| Unit tests | ✅ Complete | 11 tests covering all public methods |
| Stage velocity solver | ⏭️ Deferred | Not implemented (future enhancement) |

---

## 2. Implementation Status Update Required (for fantasim-hub)

### Action Required

Update the RFC-V2-0033 status in the `fantasim-hub` repository:

```yaml
# File: specs/rfcs/RFC-V2-0033-plate-velocity-solver.md
# Update the frontmatter status field:

status: "Implemented (MVP)"
implemented_in: "fantasim-world"
implementation_date: "2026-01-28"
```

### Current Status Location

The RFC file lives in `fantasim-hub` (not in `fantasim-world`). The canonical RFC location is:
- **Repository**: `fantasim-hub`
- **Path**: `specs/rfcs/RFC-V2-0033-plate-velocity-solver.md` (or similar)

> **Note**: RFC files are maintained in `fantasim-hub`. This document in `fantasim-world` provides the implementation notes required to update the RFC status.

---

## 3. Key Design Decisions

### 3.1 Derived Product Doctrine (RFC-V2-0033 Compliance)

Velocity products are **derived outputs** computed from kinematics truth:
- ✅ Recomputable from kinematics state
- ✅ Emit no truth events
- ✅ May be cached for performance
- ✅ Suitable for Solver Lab corpus verification

### 3.2 Determinism Guarantee

The implementation ensures deterministic results:
- Uses fixed `dt = 1 tick` for finite difference calculations
- Same inputs always produce identical outputs across platforms
- Cross-product operations follow right-hand rule conventions

### 3.3 Fallback Policy (RFC-V2-0024 Alignment)

When kinematics data is missing:
- Returns zero velocity (not throwing exceptions)
- Matches the reconstruction solver fallback policy
- Allows graceful degradation in partial data scenarios

### 3.4 Point Frame Semantics

Input points are expected in **body frame at the target tick**:
- Keeps velocity solver independent from reconstruction solver
- Caller is responsible for point transformation
- Simplifies solver implementation and testing

### 3.5 Algorithm: Finite Rotation Method

The `FiniteRotationPlateVelocitySolver` uses finite differences:

1. Get rotation `R(t)` at tick `t`
2. Get rotation `R(t + dt)` at tick `t + dt` (dt = 1 tick)
3. Compute `ΔR = R(t+dt) × R(t)⁻¹`
4. Extract angular velocity `ω` from `ΔR` using axis-angle conversion
5. Linear velocity at point `p` is `v = ω × p`

---

## 4. Verification Results

### 4.1 Units Verification

| Type | Units | Description |
|------|-------|-------------|
| `Velocity3d` | body-frame distance units per canonical tick | Linear velocity vector |
| `AngularVelocity3d` | radians per canonical tick | Axis-angle representation |
| Cross-product output | consistent with input units | `v = ω × p` |

✅ **Status**: All units consistent with simulation coordinate system.

### 4.2 Determinism Verification

| Test | Description | Result |
|------|-------------|--------|
| `GetAngularVelocity_IsDeterministic` | Same inputs produce same angular velocity | ✅ Pass |
| `GetAbsoluteVelocity_IsDeterministic` | Same inputs produce same absolute velocity | ✅ Pass |

✅ **Status**: Determinism verified through unit tests.

### 4.3 Stage Velocity Status

| Component | Status | Notes |
|-----------|--------|-------|
| `IStageVelocitySolver` interface | ⏭️ Not Implemented | Deferred to future RFC |
| Stage-level velocity queries | ⏭️ Not Implemented | Requires stage boundary definitions |
| Boundary velocity computation | ⏭️ Not Implemented | Depends on stage topology |

✅ **Status**: Stage velocity intentionally deferred. Current MVP focuses on plate-level velocities.

---

## 5. Files Created in fantasim-world

### Contracts Layer

| File | Path | Description |
|------|------|-------------|
| `Geosphere.Plate.Velocity.Contracts.csproj` | `project/contracts/Geosphere.Plate.Velocity.Contracts/` | Project file |
| `IPlateVelocitySolver.cs` | `project/contracts/Geosphere.Plate.Velocity.Contracts/` | Solver interface |
| `Velocity3d.cs` | `project/contracts/Geosphere.Plate.Velocity.Contracts/` | Velocity vector struct |
| `AngularVelocity3d.cs` | `project/contracts/Geosphere.Plate.Velocity.Contracts/` | Angular velocity struct |

### Plugin Layer

| File | Path | Description |
|------|------|-------------|
| `Geosphere.Plate.Velocity.Solver.csproj` | `project/plugins/Geosphere.Plate.Velocity.Solver/` | Project file |
| `FiniteRotationPlateVelocitySolver.cs` | `project/plugins/Geosphere.Plate.Velocity.Solver/` | Reference implementation |

### Test Layer

| File | Path | Description |
|------|------|-------------|
| `Geosphere.Plate.Velocity.Tests.csproj` | `project/tests/Geosphere.Plate.Velocity.Tests/` | Project file |
| `FiniteRotationPlateVelocitySolverTests.cs` | `project/tests/Geosphere.Plate.Velocity.Tests/` | Unit tests (11 tests) |

---

## 6. Test Results

### Summary

```
Total Tests: 11
Passed: 11
Failed: 0
Skipped: 0
```

### Test Coverage

| Test Method | Description | Category |
|-------------|-------------|----------|
| `GetAngularVelocity_ReturnsZero_WhenKinematicsReturnsFalse` | Fallback behavior for missing data | Robustness |
| `GetAbsoluteVelocity_ReturnsZero_WhenKinematicsReturnsFalse` | Fallback behavior for missing data | Robustness |
| `GetAngularVelocity_ReturnsZero_WhenRotationIsIdentity` | No rotation = zero velocity | Correctness |
| `GetAbsoluteVelocity_ReturnsZero_WhenRotationIsIdentity` | No rotation = zero velocity | Correctness |
| `GetAngularVelocity_ExtractsCorrectAxisAndRate_ForKnownRotation` | Axis-angle extraction verification | Correctness |
| `GetAbsoluteVelocity_ComputesCorrectCrossProduct` | Linear velocity from angular velocity | Correctness |
| `GetRelativeVelocity_ReturnsVelocityDifference` | Relative velocity computation | Correctness |
| `GetRelativeVelocity_ReturnsZero_WhenBothPlatesMissingKinematics` | Fallback for relative velocity | Robustness |
| `GetAngularVelocity_IsDeterministic` | Determinism guarantee | Determinism |
| `GetAbsoluteVelocity_IsDeterministic` | Determinism guarantee | Determinism |
| `AngularVelocity3d_GetLinearVelocityAt_ComputesCrossProduct` | Cross-product helper verification | Correctness |

### Running Tests

```bash
cd project
dotnet test tests/Geosphere.Plate.Velocity.Tests/Geosphere.Plate.Velocity.Tests.csproj
```

---

## 7. Action Required: Update fantasim-hub RFC File

### Step-by-Step Instructions

1. **Checkout fantasim-hub repository**:
   ```bash
   cd d:\lunar-snake\personal-work\plate-projects\fantasim-hub
   git checkout main
   git pull
   ```

2. **Locate RFC-V2-0033**:
   ```bash
   find specs/rfcs -name "*0033*" -o -name "*velocity*"
   ```

3. **Update RFC Status**:
   Edit the RFC frontmatter to reflect implementation completion:
   ```yaml
   ---
   id: "RFC-V2-0033"
   title: "Plate Velocity Solver"
   status: "Implemented (MVP)"  # <-- UPDATE THIS
   implemented_in: "fantasim-world"
   implementation_date: "2026-01-28"
   ---
   ```

4. **Add Implementation Summary Section** (if not present):
   ```markdown
   ## Implementation Summary

   This RFC has been implemented in `fantasim-world`:

   - ✅ `IPlateVelocitySolver` interface
   - ✅ `Velocity3d` and `AngularVelocity3d` value types
   - ✅ `FiniteRotationPlateVelocitySolver` reference implementation
   - ✅ 11 unit tests (all passing)
   - ⏭️ Stage velocity solver deferred to future enhancement

   See [Implementation Notes](link-to-this-doc) for details.
   ```

5. **Commit and Push**:
   ```bash
   git add specs/rfcs/RFC-V2-0033-plate-velocity-solver.md
   git commit -m "docs: Update RFC-V2-0033 status to Implemented (MVP)

   - Mark implementation as complete in fantasim-world
   - Add implementation summary section
   - Reference implementation notes in fantasim-world"
   git push
   ```

---

## Appendix: Related RFCs

| RFC | Title | Relationship |
|-----|-------|--------------|
| RFC-V2-0024 | Reconstruction Solver | Fallback policy alignment |
| RFC-V2-0012 | DES Runtime | Event sourcing context |
| RFC-V2-0015 | DES Integration Seams | Solver registration patterns |

---

*This document is a handover artifact from fantasim-world to track RFC implementation status. For the canonical RFC specification, see fantasim-hub repository.*
