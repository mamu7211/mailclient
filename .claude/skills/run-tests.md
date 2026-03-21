---
name: run-tests
description: Run unit and integration tests
user_invocable: true
---

# Run Tests

Execute tests for the Feirb solution.

## Steps

1. Run all tests:
   ```bash
   dotnet test Feirb.sln --verbosity normal
   ```

2. If a specific project or test filter is requested, use:
   ```bash
   # Specific project
   dotnet test tests/Feirb.Api.Tests --verbosity normal

   # Filter by test name
   dotnet test --filter "MethodName_or_ClassName" --verbosity normal
   ```

3. Report results: number of tests passed, failed, skipped.

4. If tests fail:
   - Show the failing test names and error messages
   - Identify the relevant source code
   - Suggest a fix if the cause is apparent
