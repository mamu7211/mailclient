---
name: implement-feature
description: Start implementing a feature from a GitHub issue
user_invocable: true
args: issue_number
---

# Implement Feature

Start working on a feature tracked by a GitHub issue.

## Steps

1. **Read the issue** from GitHub:
   ```bash
   gh issue view {issue_number}
   ```

2. **Extract the feature spec path** from the issue body (look for a link to `docs/phases/.../*.md`).

3. **Read the feature spec** to understand deliverables and acceptance criteria.

4. **Check for dependent issues** mentioned in the issue body:
   - Look for references to other issues (e.g., "Depends on: #68", "Depends on: Feature 3.1 (#68)")
   - For each referenced issue, check its state:
     ```bash
     gh issue view {dep_number} --json number,title,state
     ```
   - If any dependency is still **open**, stop and inform the user:
     - List the open dependencies with their title and number
     - Ask whether to proceed anyway, work on the dependency first, or abort
   - If all dependencies are **closed**, continue normally

5. **Check for uncommitted changes** before switching branches:
   ```bash
   git status
   ```
   - If there are uncommitted changes, stop and ask the user what to do (commit, stash, or discard).

6. **Ensure main is up to date:**
   ```bash
   git checkout main
   git pull origin main
   ```

7. **Create a feature branch from main** using the issue number and a slug derived from the issue title:
   ```bash
   git checkout -b feature/#{issue_number}-{slug}
   ```
   Example: `feature/#4-auth-infrastructure`

8. **Plan the implementation** based on the feature spec:
   - Identify files to create/modify
   - Determine the order of changes (data model → service → API → UI → tests)
   - Check for existing patterns in the codebase to follow
   - Check for existing stub/placeholder pages at the same routes — delete them before creating replacements to avoid ambiguous route errors

9. **Implement the feature** following project conventions from CLAUDE.md:
   - File-scoped namespaces, primary constructors, record DTOs
   - Minimal APIs grouped by feature
   - xUnit + FluentAssertions for tests
   - Async methods suffixed with `Async`

10. **Verify the implementation:**
   ```bash
   dotnet build Feirb.sln
   dotnet test Feirb.sln --verbosity normal
   dotnet format Feirb.sln --verify-no-changes
   ```

11. **Commit with conventional commits** referencing the issue:
    ```
    feat(api): add JWT authentication service (#4)
    ```

12. **Push and create a PR** that closes the issue:
    ```bash
    git push -u origin feature/#{issue_number}-{slug}
    gh pr create --title "..." --body "... Closes #{issue_number}"
    ```

## Notes

- Each commit message must reference the issue number
- The PR body must include `Closes #{issue_number}` to auto-close the issue on merge
- Follow the acceptance criteria from the feature spec as a checklist
- Run formatting check before committing — CI will fail otherwise
