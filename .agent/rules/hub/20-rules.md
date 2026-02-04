# Normative Rules

Rules are identified by stable IDs. Never reuse or delete IDs. Deprecated rules stay in place but are marked.

---

## R-DOC-001: Front-Matter Required

All markdown files in `docs/` (except archive, inbox, index) MUST include valid YAML front-matter.

**Required fields**: doc_id, title, doc_type, status, canonical, created, tags, summary

**Reference**: `.agent/rules/40-documentation.md`

---

## R-DOC-002: Inbox First

Agents MUST write new documentation to `docs/_inbox/` first. Never write directly to canonical locations.

**Rationale**: Prevents duplication, allows human review before promotion.

---

## R-DOC-003: Check Registry Before Creating

Before creating new documentation, agents MUST check `docs/index/registry.json` for existing canonical documents on the same topic.

**Action**: Update existing canonical doc instead of creating duplicate.

---

## R-DOC-004: Unique Canonical Per Concept

Only ONE document per concept may have `canonical: true`.

**Validation**: Enforced by documentation validation (consumer-side)

---

## R-DOC-005: Doc ID Format

Document IDs must follow format: `PREFIX-YYYY-NNNNN`

**Examples**: DOC-2025-00001, ADR-2025-00042, RFC-2025-00008

---

## R-CODE-001: No Hardcoded Secrets

Never hardcode secrets, API keys, passwords, or credentials in source code.

**Use**: Environment variables, configuration files (not in git), secure storage.

---

## R-CODE-002: Meaningful Names

Use descriptive names for variables, functions, classes. Avoid single-letter names except for standard loop counters.

---

## R-CODE-003: Comment Non-Obvious Code

Add comments to explain WHY, not WHAT. Complex algorithms should have explanatory comments.

---

## R-TST-001: Test Critical Paths

Write tests for critical game logic, state management, and core functionality.

---

## R-TST-002: Builds Must Pass

Do not commit code that breaks the build. Run tests before committing.

---

## R-GIT-001: Descriptive Commit Messages

Commit messages should clearly describe the change. Use conventional commit format when possible.

**Format**: `type(scope): description`

**Examples**:

- `feat(dungeon): add procedural map generation`
- `fix(terminal): resolve rendering issue with colors`
- `docs: update architecture documentation`

---

## R-GIT-002: No Secrets in Git

Never commit secrets, credentials, or sensitive data. Use .gitignore appropriately.

---

## R-GIT-003: Pre-Commit Hooks Required

Agents MUST NOT bypass pre-commit hooks when committing changes. Use `git commit` without `--no-verify` flag.

**Rationale**: Pre-commit hooks enforce code quality, documentation organization (R-DOC-001, R-DOC-002), security checks, and formatting standards.

**Exception**: Only bypass hooks in emergency situations and document the reason in commit message.

**Reference**: `.pre-commit-config.yaml`

---

## R-PRC-001: Document Architecture Decisions

Significant architecture decisions should be documented as ADRs in `docs/adrs/`.

---

## R-PRC-002: Document Breaking Changes

Breaking changes must be documented in CHANGELOG.md and migration guides.

---

## R-SEC-001: Validate External Input

All external input (user input, file input, network data) must be validated.

---

## R-SEC-002: Principle of Least Privilege

Run with minimum required permissions. Don't use admin/root unless necessary.

---

## R-TOOL-001: Spec-Kit for Feature Development

For new features, agents MUST use Spec-Kit workflow when appropriate.

**Workflow**: `/speckit.specify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`

**Reference**: Spec-Kit workflow (see consumer project's Taskfile.yml)

**Rationale**: Ensures consistent, documented, specification-driven development across all AI agents.

---

## R-TOOL-002: Task Runner Integration

Agents MUST use project task runner (`task` command) for common operations when available.

**Check available tasks**: `task --list`

**Reference**: `Taskfile.yml`

**Rationale**: Standardizes commands across development environments and agents.

---

## R-TOOL-003: Spec Maintenance Strategy

When updating implemented features, agents MUST follow the appropriate spec maintenance strategy.

**UPDATE existing spec when:**

- Bug fixes within original scope
- Small adjustments (< 1 day work)
- Clarifications of existing requirements
- Minor enhancements that fit existing user stories

**CREATE new spec when:**

- New major feature (> 2 days work)
- New user journeys not in original spec
- Breaking changes to core behavior
- Feature depends on but significantly extends prior spec

**Version bumps:**

- `v1.0.X` → Bug fixes and clarifications
- `v1.X.0` → Minor enhancements (new requirements, new edge cases)
- `vX.0.0` → Major changes (usually trigger new spec instead of update)

**Spec versioning format:**
Add changelog section to spec file documenting all versions and changes.

**Reference**: Spec-Kit workflow (see consumer project's Taskfile.yml)

**Rationale**: Maintains traceability between specifications and implementations while avoiding spec proliferation.

---

## R-TOOL-004: Sequential Thinking MCP (Recommended)

Agents SHOULD use the Sequential Thinking MCP server when available to record reasoning steps during development.

**Suggested usage:**

- After each meaningful action (plan, edit, run, test), append a step with: `{goal, action, result, next}`.
- On failures, record the error in `result` and propose `next`.
- Keep entries concise and non-blocking; skip if the tool is unavailable.

 **Notes:**

- This is a recommendation, not a requirement.
- Tool name may appear as `sequential-thinking` in MCP; verify availability before calling.
- Compatible with clients like Claude Code and Windsurf Cascade.

 **Reference**: Client MCP configuration / server list

 **Rationale**: Encourages transparent, auditable workflows across agents while remaining non-blocking.

 ---
