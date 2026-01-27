---
title: "SolverLab (Benchmark Harness)"
id: "solverlab"
description: ""
date: "2026-01-28"
tags: []
---
## Purpose

SolverLab is a host-side benchmarking harness for solver experimentation:

- generate or load solver corpuses
- run solvers against a shared corpus
- compare correctness (verifiers) and performance

SolverLab is not part of the stable contract surface.

## Placement

SolverLab lives under `project/hosts/`:

- `project/hosts/Geosphere.Plate.SolverLab.Core`
- `project/hosts/Geosphere.Plate.SolverLab.Runner`

Rationale:

- `project/contracts/**` is reserved for stable public contracts (Tier 1) and their Tier 2 proxies.
- SolverLab depends on benchmarking utilities and on MessagePack usage patterns that are not contract-stable.
- Keeping SolverLab in `hosts/` prevents accidental downstream dependency on benchmarking/test infrastructure.

## Dependencies

- Uses `Geosphere.Plate.Topology.Contracts` for solver inputs/outputs and shared DTOs.
- Uses `Geosphere.Plate.Topology.Serializers` for canonical MessagePack options.
- Uses `Microsoft.Extensions.Logging` (console) for host-side logging.

## Notes

- If/when we introduce Tier 2 proxies for solver-facing services, those proxies should live alongside Tier 1 interfaces in `project/contracts/**`.
