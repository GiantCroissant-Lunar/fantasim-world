Issues encountered:
- Initial dotnet build failed because the solution referenced the nested fantasim-shared path that didn't exist in repo layout.
- After initial fixes, several external project references were still missing in the environment (expected) causing many projects to be skipped during restore/build; this is outside the scope.

Resolution:
- Corrected paths to match the double-nested layout and verified dotnet can restore the FantaSim.Shared project.
