---
name: new-blazor-component
description: Scaffold a new Blazor component following project conventions
user_invocable: true
args: component_name
---

# New Blazor Component

Create a new Blazor component in `src/Feirb.Web/Components/`.

## Before Creating

1. **Check for existing components** — search `src/Feirb.Web/Components/` for a similar pattern before creating a new one. Reuse or extend existing components when possible.

2. **Consider reusability** — if the requested UI pattern is likely to appear in 2+ places, design it as a reusable component with parameters. Examples of reusable patterns:
   - `CopyableTextField` — input with copy-to-clipboard button
   - `ConfirmationModal` — reusable delete/action confirmation dialog
   - `StatusBadge` — consistent badge rendering for status indicators

## Instructions

Given a component name (from args or ask the user), create two files:

### 1. `src/Feirb.Web/Components/{ComponentName}.razor`

```razor
@namespace Feirb.Web.Components
@inject IStringLocalizer<SharedResources> L

@* Use semantic HTML elements — not just <div> soup *@
<section class="container" aria-label="@L["{ComponentName}Section"]">
    @* Component markup using Bootstrap 5 classes *@
</section>
```

### 2. `src/Feirb.Web/Components/{ComponentName}.razor.cs`

```csharp
using Microsoft.Extensions.Localization;

namespace Feirb.Web.Components;

public partial class {ComponentName} : ComponentBase
{
    [Inject]
    private IStringLocalizer<SharedResources> L { get; set; } = default!;

    // Component parameters and logic
}
```

## Conventions

### Code & Structure
- Use Bootstrap 5 classes for all styling (no custom CSS unless absolutely necessary)
- Use code-behind (`.razor.cs`) for any non-trivial logic
- Component parameters use `[Parameter]` attribute
- Event callbacks use `EventCallback<T>`
- Inject services via `[Inject]` attribute in code-behind
- Follow existing component patterns in the project
- If the component needs API data, create or reuse a typed HttpClient service in `src/Feirb.Web/Services/`
- Document component parameters and usage in a brief code comment

### WCAG / Accessibility
- Use semantic HTML elements (`<nav>`, `<main>`, `<section>`, `<article>`, `<header>`, `<footer>`, `<button>`, etc.)
- Include ARIA attributes where needed (`aria-label`, `aria-describedby`, `role`, `aria-live` for dynamic content)
- Ensure keyboard navigability — interactive elements must be focusable and have visible focus indicators
- Respect tab order — use logical DOM order, avoid positive `tabindex` values
- Do not rely on color alone to convey meaning — use icons, text, or patterns alongside color
- Form inputs must have associated `<label>` elements or `aria-label`

### i18n / Localization
- All user-facing strings must use `IStringLocalizer<SharedResources>` — no hardcoded UI text
- Inject the localizer in code-behind: `[Inject] private IStringLocalizer<SharedResources> L { get; set; } = default!;`
- Add new string keys to all `.resx` files: base (`SharedResources.resx`) + `de-DE`, `fr-FR`, `it-IT` variants in `src/Feirb.Web/Resources/`
- Use `L["KeyName"]` in `.razor` files for all labels, placeholders, messages, and aria attributes

### UX Design
- **Loading states:** show spinners or skeleton screens while data loads, use `aria-busy="true"` on loading containers
- **Error states:** display user-friendly messages with retry options, not raw exceptions
- **Form validation:** use inline validation feedback next to the relevant field, not just top-level alerts
- **Responsive design:** mobile-first approach using Bootstrap breakpoints (`col-sm-`, `col-md-`, `col-lg-`)
- **Empty states:** show a helpful message when lists/data are empty, not just a blank area

### Component Library
- Small, focused components that combine well are preferred over large monolithic ones
- Extract reusable UI patterns into shared components when a pattern appears in 2+ places
- Component parameters should have sensible defaults where possible
- Use `RenderFragment` parameters for flexible content projection

## After Creation

- Tell the user where the files were created
- List the `.resx` keys that were added (and to which files)
- Suggest where to use the component (which page or parent component)
