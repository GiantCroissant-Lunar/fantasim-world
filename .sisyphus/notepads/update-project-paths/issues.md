Issues encountered:
- The repository layout for the external repo is fantasim-shared/project/... (single directory). Earlier edits assumed a double-nested layout which was incorrect.
- Many external sibling repositories (time-dete, plugin-archi, service-archi, unify-maths, etc.) are not present in the current environment; dotnet restore skipped or reported missing projects during build. This is expected in this environment and outside the scope of this task.

Resolution:
- Updated solution and project references to point to the single-directory layout: ..\\..\\fantasim-shared\\project\\contracts\\FantaSim.Shared\\FantaSim.Shared.csproj and ensured Directory.Build.targets imports $(FantaSimSharedRoot)project\\build\\FantaSim.Shared.targets.
- Verified via dotnet build that the FantaSim.Shared project is discovered and evaluated by the build (though full build still fails due missing external repos).
