# Code Quality Rules

## Formatting Standards

- All code must pass pre-commit hooks before committing
- Follow language-specific formatting conventions:
  - **.NET**: Use `dotnet format`
  - **Python**: Use `black` and `isort`
  - **JavaScript/TypeScript**: Use `prettier`

## Documentation Requirements

- Public APIs must have documentation comments
- Complex logic should include inline comments explaining the "why"
- README files must be kept up-to-date

## Testing Standards

- New features require corresponding tests
- Bug fixes should include regression tests
- Aim for meaningful test coverage, not just high percentages

## Security

- No secrets or credentials in code
- Follow [OWASP security guidelines](https://owasp.org/www-project-top-ten/)
- Run security checks via pre-commit hooks
