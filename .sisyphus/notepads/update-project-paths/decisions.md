Decisions made during project path update

- Use relative path "..\\..\\..\\..\\..\\plate-projects\\<project>" for all external plate-projects references from fantasim-world csproj files. This keeps consistency with existing references already pointing to plate-projects.

- For fantasim-shared references (located at ..\\fantasim-shared\...), the same five-level up path is used since plate-projects is a sibling of yokan-projects.

- Only update ProjectReference Include attributes that point to the moved plate projects. Do not alter existing internal project references.

- Verified by running dotnet build; remaining errors are from unrelated missing local repositories (unify-grid) and fantasim-shared still referencing time-dete via wrong relative path which needs separate attention.
