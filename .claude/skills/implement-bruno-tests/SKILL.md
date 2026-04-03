---
name: implement-bruno-tests
description: Create Bruno API tests for existing or new endpoints
user_invocable: true
args: folder_or_feature
---

# Implement Bruno Tests

Create Bruno HTTP tests for API endpoints following the project's existing conventions.

## Steps

### Phase 1: Understand the Target

1. **If a folder name or feature is supplied**, identify the API endpoints to test:
   - Read the relevant `*Endpoints.cs` file in `src/Feirb.Api/Endpoints/`
   - Note HTTP methods, routes, request/response DTOs, status codes, and auth requirements
   - Check the DTOs in `src/Feirb.Shared/` for field names and types

2. **If no target is supplied**, ask the user what endpoints to test.

### Phase 2: Study Existing Patterns

Before writing tests, study the conventions used by existing Bruno tests:

- **Location:** `tests/bruno/` with numbered folders (`01-auth`, `02-setup`, `08-settings-labels`, etc.)
- **Folder naming:** `{nn}-{feature-slug}/` — choose the next available number
- **File naming:** `{action}.bru` (e.g., `create-label.bru`, `list-labels.bru`, `delete-label-not-found.bru`)
- **Sequence:** Each `.bru` file has `seq: N` in its `meta` block to control execution order within a folder
- **Auth:** Use `auth: bearer` with `token: {{accessToken}}` — the token is set by the auth flow
- **Variables:** Use `bru.setVar("varName", value)` in `script:post-response` to capture IDs for subsequent requests
- **Environment:** `tests/bruno/environments/local.bru` defines `baseUrl`, `adminUsername`, `adminPassword`
- **Assertions:** Use Chai-style `expect()` in `tests {}` blocks

Reference the label tests (`tests/bruno/08-settings-labels/`) as the canonical CRUD pattern:
1. `list-{resource}.bru` (seq: 1) — GET list, assert 200 + array
2. `create-{resource}.bru` (seq: 2) — POST, assert 201 + response fields, capture ID
3. `update-{resource}.bru` (seq: 3) — PUT with captured ID, assert 200 + updated fields
4. `delete-{resource}.bru` (seq: 4) — DELETE with captured ID, assert 200 + message
5. `delete-{resource}-not-found.bru` (seq: 5) — DELETE with bogus ID, assert 404

### Phase 3: Create the Test Files

1. **Create the numbered folder** in `tests/bruno/`
2. **Create `.bru` files** for each test case, following the patterns above
3. **Cover at minimum:**
   - Happy-path CRUD (list, create, update, delete)
   - 404 on non-existent resource
   - Any endpoint-specific validation (e.g., max length, required fields)
4. **Chain tests** using variables: create captures an ID, update/delete use it

### Phase 4: Verify the API is Running

**IMPORTANT:** Before running tests, verify the API is reachable:

```bash
curl -k -s -o /dev/null -w '%{http_code}' https://localhost:7272/api/setup/status
```

- If the response is `200`, proceed to run tests.
- If the connection fails (ECONNREFUSED), tell the user to start the API first:
  ```
  The API is not running. Please start it with: dotnet run --project src/Feirb.AppHost
  ```
  Do NOT attempt to run Bruno tests against a stopped API.

### Phase 5: Run the Tests

1. **IMPORTANT: Variables don't persist across separate `bru run` invocations.** The `accessToken` set by `01-auth/login.bru` only lives within a single `bru run` session. You MUST include the login request in the same run command as your tests:
   ```bash
   cd tests/bruno && npx bru run 01-auth/login.bru {folder-name}/ --env local
   ```
   Do NOT run login separately and then run your tests — every request will get 401.

2. **If individual tests fail**, read the error output, fix the `.bru` file, and re-run.

3. **Report results:** number of tests passed/failed, and any issues found.

## Tips

- **Dynamic request bodies:** Use `script:pre-request` with `req.setBody()` to generate bodies programmatically. This is useful for validation edge cases like generating a string that exceeds a max length:
  ```
  script:pre-request {
    const longValue = "a".repeat(501);
    req.setBody({ field: longValue });
  }
  ```

## Notes

- The app must be running on `https://localhost:7272` (via `dotnet run --project src/Feirb.AppHost`)
- All endpoints under `/api/*` require JWT auth except `/api/auth/*` and `/api/setup/*`
- The `accessToken` variable is set by `01-auth/login.bru` — always include it in the same `bru run` invocation
- Do NOT hardcode UUIDs for resources that may not exist — use variables from prior test steps
- Use `00000000-0000-0000-0000-000000000000` as the bogus ID for not-found tests
