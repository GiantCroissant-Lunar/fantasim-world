---
title: "Plugin Hot-Reload Doctrine (v2)"
id: "plugin-hot-reload-doctrine"
description: ""
date: "2026-01-28"
tags: []
---
This document defines the rules and long-term architecture for plugin load/unload/reload across FantaSim applications.

Scope:
- Console hosts
- Godot hosts
- Headless orchestration tools
- Future Unity hosts (HybridCLR)

This doctrine targets:
- Deterministic initialization/shutdown ordering
- Load/unload correctness (no leaks, no static coupling)
- Stable cross-plugin communication boundaries

## Goals

- Plugins can be loaded/unloaded/reloaded at runtime without destabilizing the host.
- Plugins remain isolated from each other (no accidental static state coupling).
- Cross-plugin collaboration happens via stable contracts.
- The same conceptual model works across multiple plugin repositories (e.g., `fantasim-world`, `fantasim-map`, `fantasim-view`).

## Non-goals

- Unloading contract assemblies at runtime.
- Supporting arbitrary dynamic runtime DI mutation in the host container.

## Terminology

- **Host**: the executable that owns process lifetime (console app, Godot app, tool, etc.).
- **Plugin**: a loadable module implementing a declared contract and lifecycle.
- **Contract**: a stable interface/DTO assembly that defines cross-module types.
- **ALC**: .NET `AssemblyLoadContext`.
- **Isolated ALC**: a collectible ALC used for true unload (where supported).
- **Registry**: a shared, host-owned service registry (`ServiceArchi.Contracts.IRegistry`).

## Core Rules

### R1. Contracts are shared and stable

Contracts must be loaded from a shared context (typically Default ALC). Plugins must not ship their own copies of contract assemblies into plugin directories.

Rationale:
- Prevent type-identity splits (the same interface loaded twice is not the same type).
- Prevent downcast failures and registry/DI mismatches.
- Keep the boundary stable across reload.

### R1b. Host-facing contracts must not be defined in plugin assemblies

Any contract type that the host will reference across the ALC boundary must live in a host-owned contracts assembly (loaded in Default ALC). Plugins must only implement these contracts.

Rationale:
- The shared registry is keyed by `Type`. If a plugin defines the contract interface and registers it into the host registry, the registry key can keep the plugin ALC alive even after the instance is removed.
- Moving host-facing contracts into `project/contracts/**` prevents this entire class of unload failures.

### R2. Plugins may use plugin-private DI

Each plugin may build and own an internal DI container for its private object graph.

Constraints:
- Plugin-private DI objects must not escape the plugin boundary.
- Only contract interfaces/DTOs may cross the boundary.

### R3. Cross-plugin communication is contract-only

Anything crossing between host and plugin (or plugin and plugin) must be expressed in terms of:
- Contract interfaces
- Contract DTOs
- A small set of host-owned bridge services defined in contracts

Disallowed:
- Passing concrete plugin types across the boundary
- Passing delegates capturing plugin types across the boundary

### R4. Host owns the shared registry

The host must create and register a single `ServiceArchi` registry instance and make it available to plugins.

In practice:
- Host creates `ServiceArchi.Core.ServiceRegistry`.
- Host registers it into host DI as `ServiceArchi.Contracts.IRegistry`.
- Plugins resolve `IRegistry` via their plugin context and register/unregister services into it.

### R5. Plugins must clean up on shutdown

On `ShutdownAsync` (or equivalent lifecycle shutdown), a plugin must:
- Unregister anything it registered into the shared registry.
- Unsubscribe from any host events.
- Stop background tasks/timers.
- Dispose internal DI `ServiceProvider` and any `IDisposable` resources.

### R6. Reload ordering is dependency-ordered

Initialization must follow a deterministic dependency order (topological order). Shutdown must be reverse-topological.

Rationale:
- Guarantees consumers initialize after providers.
- Guarantees providers outlive consumers.

### R7. Platform portability (Unity)

Unity/HybridCLR cannot guarantee unload at the runtime level.

Therefore:
- **Non-Unity**: Reload can mean unload+load via collectible ALC.
- **Unity**: Reload means swap active implementations (re-register), but old code may remain resident.

The boundary rules remain identical.

## Recommended Structure

- `project/contracts/**`
  - Tier 1 interfaces + DTOs
  - Tier 2 proxies (source-generated) that delegate to the registry
- `project/plugins/**`
  - Tier 3 implementations
  - Tier 4 providers (if in-process)
- `project/hosts/**`
  - Executables (console, Godot, tools)

## Option A vs Option B for cross-ALC wiring

### Option A: Registry-first only

- Host provides `IRegistry`.
- Plugins resolve `IRegistry` from context and register/unregister services.
- Consumers call services via contracts.

This works and is proven.

Risk:
- Hosts often end up injecting many host-only services into plugins directly via `IServiceProvider`.
- Over time, plugin boundaries can erode because plugins start depending on a large surface of host services.

### Option B: Introduce a HostBridge contract (recommended long-term)

Define a small contract interface that represents what plugins are allowed to depend on from the host, for example:
- `IPluginHostServices` (contract)
  - `IRegistry Registry { get; }`
  - `ILoggerFactory LoggerFactory { get; }`
  - `IClock Clock { get; }` (if needed)
  - optional: a constrained messaging bus abstraction

The host implements it and provides it to plugin DI.

#### Why Option B helps long-term

- **Surface-area control**: plugins depend on a stable, minimal host API instead of “whatever is in DI today”.
- **Backends stay swappable**: a Godot host and a console host can implement the same HostBridge differently.
- **Better tooling**: analyzers can enforce “plugins may only resolve `IPluginHostServices` (and contracts)” rather than arbitrary DI.
- **Testability**: plugin unit tests can provide a fake HostBridge without rebuilding a full host.
- **HybridCLR portability**: Unity can provide a HostBridge even if it cannot unload ALCs.

#### What Option B does not do

- It does not make contracts unloadable.
- It does not automatically prevent leaks; it makes them easier to detect by reducing accidental coupling.

## Enforcement Strategy

Hot-reload correctness is a combination of compile-time rules and runtime verification.

### 1) Compile-time enforcement (Roslyn analyzers)

Recommended analyzers (initially as warnings, later as errors in plugin projects):

- **PA0001: No static mutable state in plugin assemblies**
  - Flag `static` fields that are not `const` and not explicitly allowed.
  - Allowlist patterns:
    - `static readonly` for immutable data
    - `static readonly ILogger` is discouraged; prefer instance loggers

- **PA0002: No static events in plugins**
  - Static events commonly create leaks and prevent unload.

- **PA0003: Plugin boundary violations**
  - Disallow plugin projects referencing host projects.
  - Disallow plugin projects referencing other plugin implementation assemblies directly.

- **PA0004: Disallow returning concrete plugin types from contract interfaces**
  - Enforce that public APIs exposed across boundaries return contract types only.

Notes:
- Analyzers can catch many “obvious” cases, but cannot prove unload correctness.

### 2) Runtime verification (smoke tests)

A dedicated smoke host should:
- Load plugins via isolated ALC.
- Initialize and validate required registrations.
- Shutdown.
- Force GC and verify ALC collection.

Additionally:
- Verify `IRegistry` has no remaining registrations whose concrete type originates from the plugin ALC after shutdown.

This runtime verification is required even if analyzers exist.

### 3) CI policy

- Run smoke host in CI for:
  - plugin load
  - reload
  - unload verification

- Optionally add stress loops (N reloads).

## Appendix: Practical guidance

- Prefer instance services + explicit `Dispose`.
- Keep plugins mostly functional; push long-lived state into host services.
- Do not store host objects in statics.
- Be careful with `Task.Run` and timers; always cancel and await on shutdown.
