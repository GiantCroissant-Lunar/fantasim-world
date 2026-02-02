Changes made:
- Updated project/FantaSim.World.sln to reference the double-nested fantasim-shared path
- Updated two project references:
  - project/plugins/Geosphere.Plate.Runtime.Des/Geosphere.Plate.Runtime.Des.csproj
  - project/tests/Geosphere.Plate.Runtime.Des.Tests/Geosphere.Plate.Runtime.Des.Tests.csproj
- Updated project/Directory.Build.targets to import from $(FantaSimSharedRoot)project\build\FantaSim.Shared.targets

Why:
- The fantasim-shared repo was cloned creating fantasim-shared/fantasim-shared; paths must include extra nesting.

Successful approach:
- Searched for occurrences of FantaSim.Shared.csproj and FantaSimSharedRoot, updated occurrences precisely.

Gotchas:
- Committing triggered git hooks that normalized line endings; I re-ran commit after hooks fixed mixed line endings.
