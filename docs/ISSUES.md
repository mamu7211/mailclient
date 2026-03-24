# Issue Guidelines

## Labels

Always apply at least one type label when creating an issue:

| Label | Use for |
|-------|---------|
| `bug` | Something is broken or behaving incorrectly |
| `feature` | New user-facing functionality |
| `enhancement` | Improvement to existing functionality |
| `dev-feature` | Developer tooling, skills, CI/CD |
| `documentation` | Docs-only changes |
| `security` | Security-related issues |
| `spike` | Research / investigation tasks |

Phase labels (`phase-1`, `phase-2`, etc.) can be added to associate an issue with a project phase.

Use `planned` for issues accepted but not yet scheduled, and `specify` for issues that need more detail before implementation.

## Bug Reports

```
## Bug: <short description>

<What's wrong — 1-2 sentences>

### Steps to Reproduce

1. ...
2. ...

### Expected Behavior

<What should happen>

### Affected Pages / Components

- ...
```

## Feature Requests

Feature issues should contain the full spec inline — no separate feature spec doc needed. Structure:

```
## Feature: <short description>

<Motivation — why this is needed>

### <Section per component / area of change>

- Bullet points describing behavior

### i18n (if applicable)

- New resource keys and their values

### Out of Scope

- What this feature intentionally does NOT cover

### Dependencies

- Other issues that must be completed first, or "None"

### Testing

- What tests are expected

### Acceptance Criteria

- [ ] Checklist of deliverables
```

## General Rules

- Write in English (project language)
- Reference related issues with `#number`
- Use `Closes #number` in PR bodies to auto-close issues on merge
