---
name: add-api-endpoint
description: Scaffold a new Minimal API endpoint following project conventions
user_invocable: true
args: endpoint_description
---

# Add API Endpoint

Create a new Minimal API endpoint in `src/Feirb.Api/`.

## Instructions

Given an endpoint description (from args or ask the user), create the following:

### 1. DTOs in `src/Feirb.Shared/`

- Create request/response record types in the appropriate file or a new one
- Use record types with primary constructors
- Place in `Feirb.Shared.Models` namespace

### 2. Endpoint Group in `src/Feirb.Api/Endpoints/`

Create or extend a static class following this pattern:

```csharp
namespace Feirb.Api.Endpoints;

public static class {Feature}Endpoints
{
    public static void Map{Feature}Endpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/{feature}")
            .WithTags("{Feature}");

        group.MapGet("/", GetAllAsync);
        // ... more endpoints
    }

    private static async Task<Results<Ok<ResponseDto>, NotFound>> GetAllAsync(
        IService service,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### 3. Register in `src/Feirb.Api/Program.cs`

Add: `app.Map{Feature}Endpoints();`

### 4. Test in `tests/Feirb.Api.Tests/`

Create a test class following naming convention: `{Feature}EndpointsTests.cs`

## Conventions

- Use `Results<T1, T2>` return types for explicit HTTP response modeling
- Inject services via method parameters (Minimal API DI)
- All methods are `async Task<>` with `CancellationToken`
- Group endpoints by feature using `MapGroup`
- Add `.WithTags()` for OpenAPI grouping
- Validate inputs at the boundary
