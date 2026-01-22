---
name: core-expectations
description: Core development expectations and practices
order: 6
alwaysApply: true
---

# Core Expectations

- Prefer reading specs first (the v2 spine + referenced governance RFCs) before implementing.
- Keep contracts/IDs/event schemas stable; put algorithms and solvers behind those contracts.
- Enforce "derived stays derived": sampling/products must never become truth dependencies.
- Make changes incrementally and keep the repo runnable at each step.
- **Commit often**: Make small, focused commits after each logical change. Don't batch multiple unrelated changes.
