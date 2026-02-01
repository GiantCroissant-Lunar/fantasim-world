# Session Handover (2026-02-01): RFC-V2-0050 / 001-reconstruction-policy — Remaining Test Failures

This handover captures **test failures observed after implementing RFC-V2-0050** (ReconstructionPolicy & Scope) so they can be addressed in a follow-up change set.

## Context

- Worktree/branch: `fantasim-world--001-reconstruction-policy` / `001-reconstruction-policy`
- RFC: `fantasim-hub/docs/rfcs/v2/plates/RFC-V2-0050-reconstruction-policy-and-scope.md`
- Build status: `dotnet build project/FantaSim.World.sln -c Release` succeeds.

## Test Run

Command used:

```powershell
dotnet test project/FantaSim.World.sln -c Release --no-build -v minimal
```

## Observed Failures (unrelated to RFC-V2-0050 changes)

Reconstruction-policy related test projects were green (notably `Geosphere.Plate.Reconstruction.Tests`, `Geosphere.Plate.Velocity.Tests`, `FantaSim.World.Plates.Tests`).

Failures were seen in other domains:

- `Geosphere.Plate.Topology.Tests`
  - `Integration.EventStoreHashChainTests.*` (hash-chain continuity / mismatch)
  - `Integration.ReplayDeterminismTests.*`
  - `Integration.SolverEmissionTests.*`
- `Geosphere.Plate.Polygonization.Tests`
  - `CMap.BoundaryCMapBuilderTests.*` (determinism/signature expectations)
  - `PlatePolygonizerTests.*`
- `Geosphere.Plate.Partition.Tests`
  - `PartitionCacheTests.CacheEvictionExpired_OnlyExpiredRemoved`

These failures appear to be **existing determinism/invariant expectations** rather than missing RFC-V2-0050 implementation pieces.

## Suggested Follow-up

- Re-run with focused filters to isolate the first failing assertion and confirm if the failures are pre-existing on `main`.
- If failures reproduce on `main`, open a dedicated RFC/ADR-backed fix PR per subsystem (Topology hash chain, Polygonization determinism, Partition cache eviction).
