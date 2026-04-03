---
name: pr-review
description: Comprehensive code review on a pull request
user_invocable: true
args: pr_number
---

# PR Review — Comprehensive Pull Request Review

Perform a thorough code review on a given pull request, covering correctness, conventions, security, tests, i18n, accessibility, and UX.

## Steps

### Phase 1: Quick Review (always)

1. **Read the PR description** for context:
   ```bash
   gh pr view {pr_number} --json title,body,additions,deletions,changedFiles
   ```

2. **Get the changeset:**
   ```bash
   gh pr diff {pr_number}
   ```

3. **Analyze the diff** across these dimensions:
   - **Correctness** — logic errors, edge cases, off-by-one, null safety, race conditions
   - **CLAUDE.md conventions** — file-scoped namespaces, primary constructors, Minimal APIs (no controllers), record DTOs in `Feirb.Shared`, async suffix, expression-bodied members
   - **Security** — injection, auth bypass, secrets in code, OWASP top 10
   - **Test coverage** — are new/changed code paths covered by tests?
   - **i18n** — are all user-facing strings in `.resx` files? All locales (`en-US`, `de-DE`, `fr-FR`, `it-IT`) covered?
   - **WCAG/Accessibility** — semantic HTML, ARIA attributes, keyboard navigation, color contrast
   - **UX design** — consistent patterns, responsive layout, loading/error states, form validation feedback

4. **Determine if a deep review is needed** — complex logic, architectural changes, many files touched, or suspicious patterns warrant Phase 2.

### Phase 2: Deep Review (if needed)

1. **Check out the PR branch:**
   ```bash
   gh pr checkout {pr_number}
   ```

2. **Build the solution:**
   ```bash
   dotnet build Feirb.sln
   ```

3. **Run the tests:**
   ```bash
   dotnet test Feirb.sln --verbosity normal
   ```

4. **Check formatting:**
   ```bash
   dotnet format Feirb.sln --verify-no-changes
   ```

5. **Report build/test/format results** alongside code review findings.

### Phase 3: Output

1. **Present findings in the terminal**, grouped by category.

2. **Use severity levels:**
   - 🔴 **Blocker** — must fix before merge (bugs, security issues, broken tests)
   - 🟡 **Suggestion** — should fix, improves quality (convention violations, missing tests, i18n gaps)
   - 🔵 **Nitpick** — optional, minor improvements (style, naming preferences)
   - 🟢 **Good** — praiseworthy patterns worth calling out

3. **Prefix every finding** with its severity icon (🔴, 🟡, 🔵, or 🟢) so findings are scannable at a glance.

4. **Reference specific files and lines** when pointing out issues.

5. **End with a summary table** showing the count of each severity level:

   | Icon | Severity | Count |
   |------|----------|-------|
   | 🟢 | Good | 4 |
   | 🔵 | Nitpick | 2 |
   | 🟡 | Suggestion | 3 |
   | 🔴 | Blocker | 0 |

   Omit rows with a count of 0.

6. **After presenting findings, ask the user** what follow-up actions to take:
   - **Create an issue** for suggestions (🟡) — group them into a single issue with the appropriate label
   - **Post a PR comment** for nitpicks (🔵) — use `gh pr comment {pr_number}` to add a comment listing the nitpicks
   - **Submit a full PR review** — use `gh pr review {pr_number} --comment --body "..."` for the complete review

## Principles

- Be thorough but not pedantic — focus on things that matter
- Always read the PR description before reviewing the code
- If the diff is small and straightforward, skip Phase 2
- When in doubt about intent, check the linked issue or ask
- Praise good patterns too — not just problems
