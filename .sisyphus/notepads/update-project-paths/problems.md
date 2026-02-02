Unresolved / external problems:
- Many dependent projects live outside this repo (plate-projects, plugin-archi, time-dete). dotnet build will skip or fail to restore them if they're not present — expected in this environment.

Action for user:
- Ensure sibling repos (fantasim-shared, plate-projects, plugin-archi, time-dete, etc.) exist at paths relative to this repo, or adjust repository-level Directory.Build.props to point to alternate locations.
