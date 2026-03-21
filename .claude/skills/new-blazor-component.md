---
name: new-blazor-component
description: Scaffold a new Blazor component following project conventions
user_invocable: true
args: component_name
---

# New Blazor Component

Create a new Blazor component in `src/Feirb.Web/Components/`.

## Instructions

Given a component name (from args or ask the user), create two files:

### 1. `src/Feirb.Web/Components/{ComponentName}.razor`

```razor
@namespace Feirb.Web.Components

<div class="container">
    @* Component markup using Bootstrap 5 classes *@
</div>
```

### 2. `src/Feirb.Web/Components/{ComponentName}.razor.cs`

```csharp
namespace Feirb.Web.Components;

public partial class {ComponentName} : ComponentBase
{
    // Component parameters and logic
}
```

## Conventions

- Use Bootstrap 5 classes for all styling (no custom CSS unless absolutely necessary)
- Use code-behind (`.razor.cs`) for any non-trivial logic
- Component parameters use `[Parameter]` attribute
- Event callbacks use `EventCallback<T>`
- Inject services via `[Inject]` attribute in code-behind
- Follow existing component patterns in the project
- If the component needs API data, create or reuse a typed HttpClient service in `src/Feirb.Web/Services/`

## After Creation

- Tell the user where the files were created
- Suggest where to use the component (which page or parent component)
