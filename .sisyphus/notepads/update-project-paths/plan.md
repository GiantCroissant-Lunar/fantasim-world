# Update Project Paths

## Task

Update relative paths for referenced projects in `.csproj` files.

`fantasim-*` projects remain in `yokan-projects`.

Other projects (`time-dete`, `unify-*`, `plugin-archi`, `service-archi`) are in `plate-projects`.

## Current State

References are like `..\..\..\..\project-name\...`.

This points to `yokan-projects/project-name`.

## Target State

References for non-fantasim projects should be `..\..\..\..\..\plate-projects\project-name\...`.

## Projects to Update

- time-dete
- unify-maths
- unify-serialization
- unify-build
- plugin-archi
- service-archi
- unify-storage
- unify-ecs
- unify-topology

## Projects to Keep

- fantasim-shared (and any other fantasim-*)

## Strategy

Use `sed` to replace paths in `.csproj` files.

Pattern: `Include="..\..\..\..\<project>"` -> `Include="..\..\..\..\..\plate-projects\<project>"`
