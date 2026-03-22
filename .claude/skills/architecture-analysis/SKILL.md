---
name: architecture-analysis
description: Interactive architecture discussion about a specific aspect of the codebase
user_invocable: true
args: topic
---

# Architecture Analysis — Interactive Codebase Discussion

Facilitate an interactive discussion about a specific architectural aspect of the codebase, grounded in the actual code.

## Steps

### Phase 1: Topic Resolution

1. **If a topic/aspect is supplied** (e.g., `security`, `error-handling`, `data-access`, or a file path like `src/Feirb.Api/Endpoints/`), use it directly.

2. **If no topic is supplied**, ask the user what aspect they'd like to discuss. Suggest examples:
   - `security` — auth flow, token handling, input validation, CORS, secrets management
   - `error-handling` — consistency, user-facing vs internal errors, logging
   - `coupling` — dependency analysis, module boundaries, shared state
   - `testability` — test patterns, coverage gaps, test isolation
   - `data-access` — EF Core usage, query patterns, migrations
   - `performance` — hot paths, caching, query efficiency, lazy loading
   - Or a specific file/directory path to focus on

### Phase 2: Codebase Exploration

1. **Explore the relevant parts of the codebase** to ground the discussion in reality:
   - Read files, trace dependencies, check patterns
   - Look at how the topic manifests across the solution (`src/Feirb.Api/`, `src/Feirb.Web/`, `src/Feirb.Shared/`, `tests/`)
   - Check configuration, DI registrations, middleware pipelines where relevant

2. **Cross-reference with CLAUDE.md** to understand intended conventions vs actual implementation.

### Phase 3: Present Findings

1. **Output a structured analysis** covering:
   - **Current state** — how the codebase handles this aspect today
   - **Strengths** — what's done well, good patterns in use
   - **Weaknesses** — inconsistencies, gaps, anti-patterns
   - **Risks** — things that could cause problems as the codebase grows
   - **Refactoring opportunities** — concrete improvements worth considering

2. **Be specific** — reference files, line numbers, patterns, and code paths. Vague observations are not useful.

3. **Prioritize impact** — lead with the most important observations, don't overwhelm with minor issues.

### Phase 4: Interactive Discussion

1. **Invite the user to discuss** — ask which findings they'd like to drill into, challenge, or act on.

2. **Suggest concrete improvements** where applicable — specific refactorings, patterns to adopt, or code to restructure.

3. **This is a conversation, not a one-shot report.** Continue exploring and discussing as the user directs.

## Principles

- Be opinionated but explain the reasoning
- Ground every observation in actual code — no generic advice
- Focus on the most impactful observations, not exhaustive cataloguing
- The goal is to surface things the developer might not see from the inside
- Respect existing patterns — suggest evolution, not rewrites
