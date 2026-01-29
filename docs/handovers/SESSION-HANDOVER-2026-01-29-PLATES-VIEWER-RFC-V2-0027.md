# Session Handover — 2026-01-29 — Plates Viewer RFC-V2-0027 MVP (fantasim-app-godot)

## Objective

Implement the Plate Reconstruction Viewer MVP per **RFC-V2-0027** in `fantasim-app-godot`.

Primary outcomes achieved in this session:
- Host-authoritative **CanonicalTick** selection service.
- Viewer uses derived reconstruction via **RFC-V2-0024** `IPlateReconstructionSolver`.
- **M-axis (Model)** selection UX in the Plates Viewer.
- **Stream identity selection without environment variables** (config-backed, host authoritative).

Constraints / intent:
- Keep **sphere rendering** (no planar projection).
- Keep system generic and extensible (host-authoritative state services exposed to plugins via DI).

---

## Repos / Paths

- **fantasim-app-godot**: `D:\lunar-snake\personal-work\plate-projects\fantasim-app-godot`
  - Host: `project\hosts\fantasim-app\`
  - Shared contracts (type-shared across isolated plugin contexts): `project\contracts\FantaSim.App.Godot.Panels.Abstractions\`
  - Plates viewer plugin: `project\plugins\FantaSim.App.Plugins.PlatesViewer\`

---

## Implemented Features

### 1) Host-authoritative CanonicalTick selection

**Purpose**: Provide a single authoritative tick source for the viewer (and other panels), controlled at the host level.

- **Contract**: `fantasim-app-godot/project/contracts/FantaSim.App.Godot.Panels.Abstractions/ICanonicalTickSelection.cs`
  - `long Tick { get; }`
  - `void SetTick(long tick)`
  - `event Action<long>? TickChanged`

- **Host implementation**: `fantasim-app-godot/project/hosts/fantasim-app/Hosting/Services/CanonicalTickSelection.cs`

- **Exposure to plugins**:
  - Registered in host `AppServices.Registry`.
  - Added to plugin DI in `Bootstrap.InitializePluginHostAsync()`.

- **Viewer behavior**:
  - `PlatesViewerPanel` subscribes to `TickChanged` and keeps slider/text in sync.
  - When editing tick, it calls `SetTick` on the service (if present).

### 2) Reconstruction via RFC-V2-0024 solver contract

**Purpose**: Derived reconstruction must come from a solver contract, not ad-hoc UI math.

- **Contract**: `fantasim-world/project/contracts/Geosphere.Plate.Reconstruction.Contracts/IPlateReconstructionSolver.cs`

- **Host registration**:
  - Host registers an implementation (currently the naive solver) into `AppServices.Registry`.
  - Host project reference added so it can register the solver implementation.

- **Viewer behavior**:
  - `PlatesViewerPanel` resolves `IPlateReconstructionSolver` from `IRegistry`.
  - Calls `ReconstructBoundaries(topology, kinematics, tick)`.
  - Removed manual rotation helpers from the panel.

### 3) Stream identity selection (no env vars) + M-axis (Model) dropdown

**Purpose**: Stop using `FANTASIM_*` env vars for truth stream identity; use config-backed, host-authoritative selection with UI to choose the kinematics model (M-axis).

#### Contract boundary

- **Contract**: `fantasim-app-godot/project/contracts/FantaSim.App.Godot.Panels.Abstractions/IPlatesStreamSelection.cs`
  - `VariantId`, `BranchId`, `LLevel`, `Model`, `Models`
  - `SetModel(string model)`
  - `event Action? SelectionChanged`

Important constraint:
- `Panels.Abstractions` targets **netstandard2.1**.
- Do **NOT** reference `fantasim-world` net8 contracts from it.
  - We initially referenced `Domain`/`TruthStreamIdentity` in the abstraction; that broke build.
  - Fix applied: `IPlatesStreamSelection` is now primitive-only and the plugin constructs `TruthStreamIdentity` locally.

#### Host implementation

- **Implementation**: `fantasim-app-godot/project/hosts/fantasim-app/Hosting/Services/PlatesStreamSelection.cs`
  - Normalizes the model list (`Distinct`, trims whitespace).
  - Ensures `default_model` exists in `models`.
  - Enforces non-negative `l_level`.
  - Fires `SelectionChanged` when model changes.

#### Config

- **Loader**: `fantasim-app-godot/project/hosts/fantasim-app/Hosting/Services/FantasimConfigLoader.cs`
  - Loads from `user://fantasim.config.json` if present; else `res://fantasim.config.json`.

- **Schema**: `fantasim-app-godot/project/hosts/fantasim-app/Hosting/Services/FantasimConfig.cs`
  - Added `FantasimPlatesConfig` section.

- **Default config file**: `fantasim-app-godot/project/hosts/fantasim-app/fantasim.config.json`
  - Added:
    - `plates.variant_id`
    - `plates.branch_id`
    - `plates.l_level`
    - `plates.default_model`
    - `plates.models`

#### Viewer UX

- **Panel**: `fantasim-app-godot/project/plugins/FantaSim.App.Plugins.PlatesViewer/PlatesViewerPanel.cs`
  - Added `OptionButton` dropdown labeled `Model`.
  - Populates from `IPlatesStreamSelection.Models`.
  - On selection, calls `IPlatesStreamSelection.SetModel` and refreshes.
  - Builds truth streams locally:
    - `Domain.GeoPlatesTopology`
    - `Domain.GeoPlatesKinematics`
    using `new TruthStreamIdentity(VariantId, BranchId, LLevel, domain, Model)`.
  - Removed env-var based stream fallback logic.

---

## Deterministic ordering notes

- Current ordering for topology geometries in the panel is:
  - `topology.Boundaries.Values.Where(!retired).OrderBy(BoundaryId.Value)`
- Reconstruction ordering is currently whatever the solver returns.

---

## Build Status

Builds succeed locally (warnings exist in downstream projects but no errors).

- Build host:
  - `dotnet build` in `fantasim-app-godot/project/hosts/fantasim-app`

- Build plugin:
  - `dotnet build` in `fantasim-app-godot/project/plugins/FantaSim.App.Plugins.PlatesViewer`

---

## Key Files (Quick Index)

### fantasim-app-godot

- Host:
  - `project/hosts/fantasim-app/Bootstrap.cs` (register + expose services to plugins)
  - `project/hosts/fantasim-app/Hosting/Services/CanonicalTickSelection.cs`
  - `project/hosts/fantasim-app/Hosting/Services/PlatesStreamSelection.cs`
  - `project/hosts/fantasim-app/Hosting/Services/FantasimConfig.cs`
  - `project/hosts/fantasim-app/Hosting/Services/FantasimConfigLoader.cs`
  - `project/hosts/fantasim-app/fantasim.config.json`

- Contracts (shared / type-shared):
  - `project/contracts/FantaSim.App.Godot.Panels.Abstractions/ICanonicalTickSelection.cs`
  - `project/contracts/FantaSim.App.Godot.Panels.Abstractions/IPlatesStreamSelection.cs`

- Plates Viewer plugin:
  - `project/plugins/FantaSim.App.Plugins.PlatesViewer/PlatesViewerPanel.cs`
  - `project/plugins/FantaSim.App.Plugins.PlatesViewer/PlatesViewerCanvas.cs`

### fantasim-world

- Reconstruction solver contract:
  - `project/contracts/Geosphere.Plate.Reconstruction.Contracts/IPlateReconstructionSolver.cs`
- Naive solver implementation (currently used by the host):
  - `project/plugins/Geosphere.Plate.Reconstruction.Solver/NaivePlateReconstructionSolver.cs`

---

## Known follow-ups / optional extensions

- Persist user-selected model back to `user://fantasim.config.json` (currently runtime-only).
- Add UI for `VariantId`, `BranchId`, `LLevel` selection (currently config-only).
- Define and codify deterministic ordering for reconstructed boundaries (if required by viewer UX).
