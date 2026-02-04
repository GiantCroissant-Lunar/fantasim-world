# .NET Architecture Rules

**CRITICAL**: All agents working on .NET projects MUST follow these architecture rules.

## Quick Reference

### Four-Tier Service Architecture

```
Tier 1: Contracts (Interfaces)        → What the service does
Tier 2: Proxies (Generated)           → Routing to implementation
Tier 3: Real Services (Plugins)       → How it's implemented
Tier 4: Providers (Optional)          → Internal strategies
```

### Golden Rules

1. **NO WRAPPER PROJECTS** - Use external libraries directly in plugins
2. **CONTRACTS ARE STABLE** - Tier 1 never depends on implementations
3. **PLUGINS ARE ISOLATED** - Plugins never depend on other plugins
4. **SEPARATE DOMAIN AND PLATFORM** - Domain plugins (WHAT) ≠ Platform plugins (HOW)
5. **SHARED LIBS ARE BUILDING BLOCKS** - Algorithms and models, not services

---

## Tier Dependency Rules

### ✅ ALLOWED Dependencies

- Tier 2 → Tier 1
- Tier 3 → Tier 1
- Tier 3 → Shared libraries
- Tier 3 → External libraries (directly, no wrappers!)
- Tier 4 → Tier 1
- game-essential → app-essential
- projects → app-essential + game-essential
- Plugins → Contracts + Shared

### ❌ FORBIDDEN Dependencies

- Tier 1 → Any other tier
- Tier 2 → Tier 3 or Tier 4 (except via `IRegistry`)
- Tier 3 → Tier 2
- Tier 3 → Other Tier 3 implementations
- app-essential → game-essential
- Contracts → Shared
- Plugin → Plugin

---

## Project Naming Conventions

### Contracts (Tier 1)

- **App-level**: `PigeonPea.Contracts.<Domain>`
- **Game-level**: `PigeonPea.Game.Contracts.<Domain>`

### Shared Libraries

- `PigeonPea.Shared.<Domain>`

### Plugins (Tier 3)

- **App-level**: `PigeonPea.Plugins.<Domain>.<Implementation>`
- **Game-level**: `PigeonPea.Plugins.<Domain>.<Implementation>`
- **Project-specific**: `PigeonPea.Plugin.<Domain>.<Implementation>` (singular)

### Examples

- ✅ `PigeonPea.Contracts.Input`
- ✅ `PigeonPea.Shared.Inventory`
- ✅ `PigeonPea.Plugins.Audio.LibVlc`
- ✅ `PigeonPea.Plugin.Dungeon.ModernEdgar`
- ❌ `PigeonPea.Dungeon.Core` (wrapper - forbidden!)

---

## Project Location Rules

### App-Essential (Non-Gameplay)

**Purpose**: Infrastructure used by any application (console, windows, tools)

**Contracts**: `dotnet/app-essential/core/src/PigeonPea.Contracts.<Domain>/`

**Shared**: `dotnet/app-essential/core/src/PigeonPea.Shared.<Domain>/` or `dotnet/engine/core/src/` (transitional)

**Plugins**: `dotnet/app-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Name>/`

**Examples**: Audio, Input, Config, Resource

### Game-Essential (Gameplay)

**Purpose**: Game capabilities reusable across different games

**Contracts**: `dotnet/game-essential/core/src/PigeonPea.Game.Contracts.<Domain>/`

**Shared**: `dotnet/game-essential/core/src/PigeonPea.Shared.<Domain>/`

**ECS Integration**: `dotnet/game-essential/core/src/PigeonPea.Game.<Domain>/`

**Plugins**: `dotnet/game-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Name>/`

**Examples**: Inventory, GAS, Perception, AI

### Content Domains (Project-Specific)

**Purpose**: Domain-specific implementations with rendering

**Contracts**: `dotnet/game-essential/core/src/PigeonPea.<Domain>.Contracts/`

**Plugins**: `projects/<domain>/dotnet/<project>/plugins/PigeonPea.Plugin.<Domain>.<Feature>/`

**Examples**: Map, Dungeon

**CRITICAL**: Content domains use **Double-Plugin Architecture**:

- Domain plugins (know WHAT to render)
- Platform plugins (know HOW to render)

---

## Double-Plugin Architecture for Content Domains

### Problem

How do we render dungeons on different platforms (ANSI, Braille, SkiaSharp)?

### Solution

Two types of plugins that don't know about each other:

```
┌─────────────────────────────────────┐
│ Domain Plugin (WHAT to render)     │
│ - PigeonPea.Plugin.Dungeon.Rendering│
│ - Calls: IRenderer.DrawTile()      │
└─────────────────────────────────────┘
              ↓ uses
┌─────────────────────────────────────┐
│ IRenderer (Tier 1 Contract)        │
└─────────────────────────────────────┘
              ↑ implements
┌─────────────────────────────────────┐
│ Platform Plugins (HOW to render)   │
│ - Plugin.Rendering.Terminal.ANSI   │
│ - Plugin.Rendering.Terminal.Braille│
│ - Plugin.Rendering.Windows.SkiaSharp│
└─────────────────────────────────────┘
```

### Rules

**Domain Plugins**:

- ✅ Know domain semantics (walls, floors, doors)
- ✅ Call `IRenderer.DrawTile(x, y, tile)`
- ❌ NO platform-specific code (ANSI escapes, Win32 APIs)
- ❌ NO knowledge of platform plugins

**Platform Plugins**:

- ✅ Implement `IRenderer` interface
- ✅ Know platform-specific rendering (ANSI, Braille, etc.)
- ❌ NO domain-specific knowledge (dungeon, map)
- ❌ NO knowledge of domain plugins

**Result**: Add new domain → works with all platforms. Add new platform → works with all domains!

---

## Critical Anti-Patterns to AVOID

### ❌ Anti-Pattern 1: Wrapper Projects

**WRONG**:

```
PigeonPea.Dungeon.Core/
└── ModernEdgarWrapper.cs  // Wraps modern-edgar-dotnet
```

**RIGHT**:

```
PigeonPea.Plugin.Dungeon.ModernEdgar/
└── ModernEdgarDungeonGenerator.cs  // Uses modern-edgar-dotnet DIRECTLY
```

**Why**: Wrappers violate tier architecture and create ALC type identity issues.

### ❌ Anti-Pattern 2: Plugin Depending on Plugin

**WRONG**:

```xml
<!-- In PigeonPea.Plugin.A -->
<ProjectReference Include="..\PigeonPea.Plugin.B\PigeonPea.Plugin.B.csproj" />
```

**RIGHT**:

```csharp
// Both plugins implement same contract
// Consumer uses registry
var service = _registry.Get<IService>();
```

**Why**: Plugins must be isolated for separate ALC loading.

### ❌ Anti-Pattern 3: Heavy Dependencies in Contracts

**WRONG**:

```csharp
// In PigeonPea.Contracts
using Newtonsoft.Json;
public interface IService { JObject GetData(); }
```

**RIGHT**:

```csharp
// In PigeonPea.Contracts
public interface IService { Dictionary<string, object> GetData(); }
```

**Why**: Contracts must be lightweight and stable.

### ❌ Anti-Pattern 4: Business Logic in Proxies

**WRONG**:

```csharp
public class Service : IService
{
    public Result DoSomething()
    {
        var result = ComplexCalculation(); // ❌ Logic in proxy
        return result;
    }
}
```

**RIGHT**:

```csharp
public class Service : IService
{
    private readonly IRegistry _registry;
    public Result DoSomething()
    {
        var impl = _registry.Get<IService>();
        return impl.DoSomething(); // ✅ Delegate only
    }
}
```

**Why**: Proxies are routing only; logic belongs in Tier 3.

### ❌ Anti-Pattern 5: Mixing Domain and Platform

**WRONG**:

```csharp
public class DungeonANSIRenderer // ❌ Coupled to both
{
    public void Render(DungeonView dungeon)
    {
        Console.Write("\x1b[2J"); // ANSI
        // Dungeon rendering...
    }
}
```

**RIGHT**:

```csharp
// Domain plugin
public class DungeonRenderer
{
    private readonly IRenderer _renderer; // ✅ Platform-agnostic
    public void Render(DungeonView dungeon)
    {
        _renderer.DrawTile(x, y, tile);
    }
}

// Platform plugin (separate!)
public class ANSIRenderer : IRenderer
{
    public void DrawTile(int x, int y, Tile tile) { /* ANSI */ }
}
```

**Why**: Separation allows reusing renderers across domains.

---

## Implementation Checklist

### Creating New Service Category

1. **Define Tier 1 Contract**
   - Location: `PigeonPea[.Game].Contracts.<Domain>/Services/IService.cs`
   - Small, focused interface
   - Only primitives or DTOs from same assembly
   - No implementation dependencies

2. **Create Tier 2 Proxy**
   - Location: `Services/Proxy/Service.cs`
   - Decorate with `[RealizeService(typeof(IService))]`
   - Delegate to `IRegistry.Get<IService>()`
   - No business logic

3. **Create Shared Library (if needed)**
   - Location: `PigeonPea.Shared.<Domain>/`
   - Domain models and algorithms
   - No plugin system knowledge
   - No `IRegistry` dependencies

4. **Create Plugin Implementation**
   - Location: `PigeonPea.Plugins.<Domain>.<Name>/`
   - Implement Tier 1 interface
   - Register in `IPlugin.InitializeAsync()`
   - Create `plugin.json` manifest

5. **Register in ALC Config**
   - Add to `PluginSystem:SharedAssemblies` if contract assembly
   - Set correct `supportedProfiles` in `plugin.json`

### Before Creating New Code - Decision Tree

```
Is this infrastructure (input, audio, config)?
  YES → app-essential
  NO  → Is this gameplay (inventory, AI, perception)?
          YES → game-essential
          NO  → Is this domain-specific (dungeon, map)?
                  YES → projects/<domain>

Does a contract exist for this?
  YES → Create plugin implementing it
  NO  → Do I need swappable implementations?
          YES → Create Tier 1 + 2 + 3
          NO  → Create shared library only

Do I need to wrap an external library?
  NO  → Use external library DIRECTLY in plugin
  YES → STOP! Wrappers are forbidden. Use directly.

Is this rendering-related?
  YES → Use Double-Plugin Architecture
          - Domain plugin (WHAT)
          - Platform plugin (HOW)
  NO  → Standard four-tier architecture
```

---

## ALC Configuration Rules

### Shared Assemblies

Contracts MUST be loaded from Default ALC to ensure type identity.

**Configuration** (`appsettings.json`):

```json
{
  "PluginSystem": {
    "SharedAssemblies": [
      "PigeonPea.Contracts",
      "PigeonPea.Game.Contracts",
      "PigeonPea.Rendering.Contracts",
      "PigeonPea.Dungeon.Contracts",
      "PigeonPea.Map.Contracts",
      "PigeonPea.Shared.Inventory",
      "Arch"
    ]
  }
}
```

**Rule**: Add any Tier 1 contract assembly to this list.

### Plugin Manifest

**Required fields** (`plugin.json`):

```json
{
  "id": "unique-plugin-id",
  "name": "Human Readable Name",
  "version": "1.0.0",
  "capabilities": ["feature-tag"],
  "supportedProfiles": ["dotnet.console", "dotnet.windows"],
  "entryPoint": {
    "dotnet.console": "AssemblyName.dll,Namespace.PluginClass"
  }
}
```

**Rule**: Host loads only plugins matching its profile.

---

## Quick Reference: What Goes Where?

### Contracts (Tier 1)

**Purpose**: Define service API
**Location**: `PigeonPea[.Game].Contracts.<Domain>/Services/`
**Contains**:

- Interface definitions
- DTOs and primitives
- Enums and constants
  **Does NOT contain**:
- Implementations
- Business logic
- Heavy dependencies

### Shared Libraries

**Purpose**: Reusable domain logic
**Location**: `PigeonPea.Shared.<Domain>/`
**Contains**:

- Domain models
- Algorithms
- Utilities
- Data structures
  **Does NOT contain**:
- Plugin knowledge
- `IRegistry` usage
- Service contracts

### Plugins (Tier 3)

**Purpose**: Service implementation
**Location**: `PigeonPea.Plugins.<Domain>.<Name>/`
**Contains**:

- Service implementation
- Plugin class (`IPlugin`)
- External library usage
- `plugin.json` manifest
  **Does NOT contain**:
- Dependencies on other plugins
- Wrapper projects
- Contract definitions

### ECS Integration

**Purpose**: Tie domain to ECS
**Location**: `PigeonPea.Game.<Domain>/`
**Contains**:

- Components
- Systems
- ECS queries
- Event publishers/subscribers
  **Does NOT contain**:
- Service contracts
- Plugin code
- Platform-specific code

---

## When to Ask for Guidance

Ask the user or check documentation when:

1. **Unsure about location**: "Should this be app-essential or game-essential?"
2. **Unclear tier assignment**: "Is this Tier 1 contract or Tier 3 plugin?"
3. **External library integration**: "Should I create a wrapper?" (Answer: NO, but confirm)
4. **Domain vs Platform**: "Does this belong in domain or platform plugin?"
5. **Breaking changes**: "This would change a Tier 1 contract, how to handle?"

**Default action**: Follow this guide. When in doubt, prefer:

- Plugin over wrapper
- Shared library over contract (for algorithms)
- Separate domain and platform plugins
- Direct external library usage

---

## Enforcement

### Pre-Code Review Checklist

Before submitting PR or asking for review:

- [ ] No wrapper projects created
- [ ] Plugins don't depend on other plugins
- [ ] Contracts are in correct location
- [ ] Shared libraries have no plugin knowledge
- [ ] External libraries used directly (no wrappers)
- [ ] Domain and platform concerns separated
- [ ] `plugin.json` present for all plugins
- [ ] ALC config updated if new contracts added
- [ ] Naming conventions followed
- [ ] Dependency rules respected

### Common Review Feedback

If you receive feedback like:

- "This should be a plugin, not a wrapper" → Review Anti-Pattern 1
- "Plugins can't depend on each other" → Review Anti-Pattern 2
- "Contracts should be lighter" → Review Anti-Pattern 3
- "Separate domain from platform" → Review Double-Plugin Architecture
- "Use external library directly" → Remove wrapper, reference directly

---

## Additional Resources

**Comprehensive Guide**:

- [.NET Tiered Architecture and Layer Implementation Guide](../../docs/guides/dotnet-tiered-architecture-guide.md)

**Related RFCs and ADRs**:

- [RFC-013: Plugin Architecture Refinement](../../docs/rfcs/013-plugin-architecture-refinement-tiered.md)
- [ADR-001: Architecture Overview](../../docs/dotnet/architecture/overview.md)
- [ADR-003: Service Tiers](../../docs/dotnet/architecture/service-tiers.md)
- [ADR-004: Services and Plugins](../../docs/dotnet/architecture/services-and-plugins.md)

**Questions?**

- Check the comprehensive guide first
- Review related RFCs/ADRs
- Ask the user if still unclear

---

## Summary

**Remember the Golden Rules**:

1. NO wrapper projects
2. Contracts are stable
3. Plugins are isolated
4. Separate domain and platform
5. Shared libraries are building blocks

**When in doubt**:

1. Check if a contract exists
2. Use external libraries directly
3. Create a plugin, not a wrapper
4. Keep domains and platforms separate
5. Follow the decision tree above

**Most Important**:

- Read [.NET Tiered Architecture Guide](../../docs/guides/dotnet-tiered-architecture-guide.md) for complete details
- This is a living document - check for updates
- Architecture consistency > quick fixes
