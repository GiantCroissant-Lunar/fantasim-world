---
name: tiered-service-architecture
description: Tiered service architecture (T1 interfaces, T2 proxies, T3 services, T4 providers)
order: 10
---

# Tiered Service Architecture

This repo must follow the canonical service tiering doctrine.

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0007-tiered-service-architecture.md`

Rules:

- Tier 1 (T1) service interfaces belong in `project/contracts/**`.
- Tier 2 (T2) proxies are co-located with Tier 1 in `project/contracts/**` and must resolve via `ServiceArchi.Contracts.IRegistry` (prefer source-generated proxies).
- Tier 2 must not reference Tier 3 implementations or Tier 4 providers.
- Tier 3 (T3) implementations belong in `project/plugins/**`.
- Tier 4 (T4) providers (storage/network/etc.) must not leak above T3.
