Changes made:
- Reverted references that previously pointed to the incorrect double-nested path and updated them to the single-directory layout.
- Updated project/FantaSim.World.sln to reference "..\\..\\fantasim-shared\\project\\contracts\\FantaSim.Shared\\FantaSim.Shared.csproj"
- Updated two project references:
  - project/plugins/Geosphere.Plate.Runtime.Des/Geosphere.Plate.Runtime.Des.csproj
  - project/tests/Geosphere.Plate.Runtime.Des.Tests/Geosphere.Plate.Runtime.Des.Tests.csproj
- Ensured project/Directory.Build.targets sets FantaSimSharedRoot to "$(MSBuildThisFileDirectory)..\\..\\fantasim-shared\\" and imports $(FantaSimSharedRoot)project\\build\\FantaSim.Shared.targets

Why:
- The fantasim-shared repo was added with a single directory structure: fantasim-shared/project/..., not fantasim-shared/fantasim-shared/project/...

Successful approach:
- Searched for exact occurrences of FantaSim.Shared.csproj and FantaSimSharedRoot, updated only those references.

Gotchas:
- Earlier edits (by a teammate or prior run) introduced a wrong double-nested path; this change restores the intended single-directory layout.
