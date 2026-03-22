---
name: grill-me
description: Rigorous design tree exploration interview for feature analysis
user_invocable: true
args: feature_description
---

# Grill Me — Design Tree Exploration

Rigorously interview the user about a feature idea, walking down every branch of the design tree until a shared understanding is reached.

## Steps

1. **Get the feature idea:**
   - If `{feature_description}` is provided, use it as the starting point
   - Otherwise, ask the user to describe their feature idea in a few sentences

2. **Interview relentlessly:**
   - Identify the top-level branches of the design tree (user stories, data model, API, UI/UX, security, performance, i18n, edge cases, error scenarios, accessibility)
   - Work through one branch at a time — don't jump between topics
   - For each branch, drill down until the decision is resolved or explicitly deferred
   - Resolve dependencies between decisions: if a UI question depends on a data model decision, settle the data model first
   - Ask focused, specific questions — not vague open-ended ones
   - Challenge assumptions and point out contradictions
   - If a question can be answered by exploring the codebase (e.g., existing patterns, available services, data model), explore the codebase instead of asking the user

3. **Keep it conversational:**
   - Ask 1-3 questions per message, not a wall of questions
   - Summarize what was decided before moving to the next branch
   - Acknowledge when a branch is fully explored before moving on
   - Respect the user's time — skip dimensions that are obviously not relevant

4. **Produce a summary** once all branches are explored:
   - Structured summary of all decisions made, grouped by dimension
   - Scope: what's in, what's explicitly out
   - Requirements and constraints
   - Open questions (if any remain)
   - Suggested placement: new phase, new feature spec, or addition to existing feature

5. **Offer next steps:**
   - Create a GitHub issue from the summary
   - Create a feature spec document in `docs/phases/`
   - Or just leave it as a conversation artifact

## Principles

- This skill compensates for the tendency to jump straight to implementation
- Concise questions with precise language
- Don't ask questions the codebase can answer — explore first
- The goal is shared understanding, not a checklist
- It's OK to say "this branch doesn't apply" and move on
