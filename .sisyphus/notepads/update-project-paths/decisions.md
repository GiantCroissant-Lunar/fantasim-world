Decisions:
- Use the actual filesystem layout: fantasim-shared/fantasim-shared (double nesting). Reverted earlier partial change which removed the duplication; final state keeps double nesting because the shared repo physically contains nested folder.
- Only update files directly mentioning FantaSim.Shared.csproj or FantaSimSharedRoot. Do not change other project paths.
