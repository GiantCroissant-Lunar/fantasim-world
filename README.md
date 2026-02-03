# FantaSim.World

[![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/GiantCroissant-Lunar/fantasim-world)](https://coderabbit.ai)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![CSharp](https://img.shields.io/badge/language-C%23-512BD4.svg)](https://learn.microsoft.com/en-us/dotnet/csharp)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-FFDD00?logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/apprenticegc)

This repository implements a modern simulation framework with:

- **Topology-First Architecture**: Plate topology as the authoritative truth source
- **DB-First Persistence**: RocksDB with MessagePack canonical encoding
- **In-Memory Graph Engine**: ModernSatsuma for topology materialization
- **Spec-Driven Development**: GitHub Spec Kit for structured feature development

## Key Technologies

| Component | Technology |
|-----------|------------|
| Language | C# / .NET |
| Persistence | RocksDB via modern-rocksdb |
| Encoding | MessagePack |
| Graph Engine | Plate.ModernSatsuma |
| Development | GitHub Spec Kit |

## Quick Start

```bash
# Clone the repository
git clone <repository-url>
cd fantasim-world

# Sync skills (after cloning/pulling)
task sync-skills

# Build the project
task build

# Run tests
task test
```

## Spec-Driven Development

This project uses **GitHub Spec Kit** for spec-driven development.

### Create a New Spec

```bash
# 1. Create worktree for new spec
task spec:new -- feature-name

# 2. Enter worktree
cd ../fantasim-world--feature-name

# 3. Run spec phases
task spec:specify FEATURE=feature-name
task spec:plan FEATURE=feature-name
task spec:tasks FEATURE=feature-name

# 4. Implement tasks, commit with task IDs

# 5. Create PR, merge, cleanup
task spec:done -- feature-name
```

## Architecture

### Two Vocabularies

This project maintains two distinct vocabularies that must not be conflated:

- **Governance / identity axes**: `Variant`, `Branch`, `L`, `R`, `M`
- **Pipeline layering**: `Topology`, `Sampling`, `View/Product`

### Topology-First Doctrine

For the plates domain:
- **Authoritative truth** is **Plate Topology** (boundary graph + events)
- **Spatial substrates** (Voronoi/DGGS/cell meshes, cell-to-plate assignment) are **derived sampling products**

### Persistence

- **Backend**: RocksDB via modern-rocksdb
- **Canonical encoding**: MessagePack (DB-first; JSON is export/import only)

## Documentation

- **Canonical Specs**: See [`../fantasim-hub/docs/rfcs/`](../fantasim-hub/docs/rfcs/)
- **v2 Topology-First Spine**: [`../fantasim-hub/docs/rfcs/v2/RFC-INDEX.md`](../fantasim-hub/docs/rfcs/v2/RFC-INDEX.md)
- **Terminology**: [`../fantasim-hub/docs/TERMINOLOGY.md`](../fantasim-hub/docs/TERMINOLOGY.md)

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
