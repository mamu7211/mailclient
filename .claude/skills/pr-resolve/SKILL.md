---
name: pr-resolve
description: Address unresolved PR review comments — fix code, reply, and resolve threads
user_invocable: true
args: pr_number
---

# PR Resolve

Read unresolved review comments on a PR, fix the code, reply to each comment, and resolve the threads.

## Steps

### Phase 1: Gather Unresolved Feedback

1. **Ensure you're on the correct branch:**
   ```bash
   gh pr view {pr_number} --json headRefName --jq '.headRefName'
   ```
   Check out the branch if not already on it.

2. **Fetch all PR review comments:**
   ```bash
   gh api repos/{owner}/{repo}/pulls/{pr_number}/comments \
     --jq '.[] | {id: .id, path: .path, line: .line, body: .body, user: .user.login}'
   ```

3. **Identify unresolved threads** via GraphQL:
   ```bash
   gh api graphql -f query='
   {
     repository(owner: "{owner}", name: "{repo}") {
       pullRequest(number: {pr_number}) {
         reviewThreads(first: 50) {
           nodes {
             id
             isResolved
             comments(first: 1) {
               nodes { id, body, path, line: originalLine }
             }
           }
         }
       }
     }
   }' --jq '.data.repository.pullRequest.reviewThreads.nodes[] | select(.isResolved == false)'
   ```

4. **Skip if no unresolved threads.** Inform the user and stop.

### Phase 2: Triage and Fix

For each unresolved comment:

1. **Read the comment** — understand what the reviewer is asking for.
2. **Read the relevant source file** at the referenced path and line.
3. **Assess the feedback:**
   - **Valid and actionable** — fix the code.
   - **Valid but out of scope** — reply explaining why, don't fix.
   - **Incorrect or based on a misunderstanding** — reply with explanation.
4. **Apply the fix** if needed, following project conventions from CLAUDE.md.

Group related comments (e.g., multiple comments about the same pattern) and fix them together.

5. **Classify each comment** into one of these categories:
   - **Trivial** — style, naming, minor improvements
   - **Non-trivial** — architecture, security, or data consistency implications

### Phase 3: Triage Summary

**STOP and present a summary to the user before applying any fixes.** For each unresolved comment, show:

| # | File:Line | Category | Reviewer | Action | Description |
|---|-----------|----------|----------|--------|-------------|
| 1 | `path:line` | Trivial/Non-trivial | user | Fix / Skip / Discuss | One-line summary |

For non-trivial items, add a brief note on the architectural, security, or data consistency implication.

**Wait for the user to approve** before proceeding. The user may:
- Approve all — proceed with all fixes
- Skip specific items — exclude them from the fix commit, reply explaining why
- Request discussion — talk through a specific item before deciding

### Phase 4: Apply Fixes

Apply only the approved fixes, following project conventions from CLAUDE.md.

### Phase 5: Verify

After all fixes are applied:

```bash
dotnet build Feirb.sln
dotnet test Feirb.sln --verbosity normal
dotnet format Feirb.sln --verify-no-changes
```

### Phase 6: Commit and Push

Commit all fixes in a single commit referencing the issue:

```
fix(scope): address PR review feedback for feature-name (#issue)
```

Push to the PR branch.

### Phase 7: Reply and Resolve

For each unresolved thread:

1. **Reply** with a short message referencing the fix commit:
   ```bash
   gh api repos/{owner}/{repo}/pulls/{pr_number}/comments/{comment_id}/replies \
     -f body="Fixed in {commit_sha} — {brief description of what changed}."
   ```

2. **Resolve the thread** via GraphQL:
   ```bash
   gh api graphql -f query='
   mutation {
     resolveReviewThread(input: {threadId: "{thread_id}"}) {
       thread { isResolved }
     }
   }'
   ```

Batch multiple resolve mutations into a single GraphQL call when possible.

### Phase 8: Summary (if non-trivial fixes exist)

If any comment was classified as **non-trivial** (architecture, security, or data consistency), present a summary to the user after all threads are resolved:

**For each non-trivial fix, include:**
- **What the reviewer flagged** — one-line description of the issue
- **What was changed** — the fix applied, with file and line reference
- **Why it matters** — the architectural, security, or data consistency implication
- **Residual risk** — whether the fix fully addresses the concern, or if there's follow-up work (e.g., the same pattern exists elsewhere in the codebase)

This summary helps the user understand changes that go beyond cosmetics and may affect other parts of the system.

## Notes

- Always verify you're on the PR branch before making changes
- Don't blindly apply suggested code — review it for correctness and convention compliance first
- If a reviewer's suggestion conflicts with project conventions, follow the conventions and explain why
- Reply to every unresolved comment, even if no code change is needed
