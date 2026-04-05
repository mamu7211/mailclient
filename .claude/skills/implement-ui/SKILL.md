---
name: implement-ui
description: Implement UI work using the shared component library with consistent styling enforcement
user_invocable: true
args: description_of_ui_work
---

# Implement UI

Implement UI work using registered primitives from the component library. Enforces consistent styling across all pages.

## Steps

1. **Read the component registry:**
   ```
   docs/component-library.md
   ```

2. **Check for existing components** — search `src/Feirb.Web/Components/` for similar patterns before creating new ones. Reuse or extend existing components when possible.

3. **Read the page/component context** — understand what exists, what needs to change.

4. **Draft a short plan** for the UI work:
   - Which primitives will be used
   - Which elements need to change
   - Any new primitives needed
   - Ask the user to confirm before proceeding

5. **Implement using registered primitives:**
   - Replace raw Bootstrap button classes with `<Button>` or `<CircularButton>`
   - Replace raw `<i class="bi bi-*">` with `<Icon>`
   - Replace raw card markup with `<Card>`
   - Follow existing page patterns for layout and structure

6. **WCAG AA check:**
   - All interactive elements are keyboard-focusable
   - `aria-label` on icon-only buttons (CircularButton enforces this)
   - Contrast meets 4.5:1 for text, 3:1 for large text and UI components
   - Semantic HTML elements where appropriate
   - No color-only indicators

7. **Responsive check:**
   - Desktop primary, mobile supported
   - Use Bootstrap breakpoints (`col-sm-`, `col-md-`, `col-lg-`) for layout
   - Test that content doesn't overflow on small screens

8. **If a small primitive is missing** (e.g., a Badge, Divider, or similar):
   - Pause implementation
   - Propose the primitive with 2-3 design questions:
     - What variants does it need?
     - What sizes?
     - Any accessibility requirements?
   - After user confirms, create it in `src/Feirb.Web/Components/UI/`
   - Add styles to `app.css` with `feirb-` prefix
   - Update `docs/component-library.md`
   - Continue implementation

9. **If a large primitive is missing** (e.g., Modal, DataTable, Toolbar redesign):
    - Propose a GitHub issue for the primitive
    - User decides: create issue and defer, or build it now
    - If building now, follow the same pattern as step 8 but with full design exploration

10. **If UX exploration is needed** (interaction patterns, layout decisions, responsive behavior):
    - Suggest using `/designer-grill-me` for design exploration
    - Wait for design decisions before implementing

## Hard Rules

- **If a primitive exists, use it** — never emit raw Bootstrap classes for elements covered by the library
- Non-covered elements (modals, forms, tables, menus): use Bootstrap classes but encourage componentization
- If a non-covered pattern appears 2+ times, create a refactoring issue
- All icon-only buttons MUST use `<CircularButton>` with `AriaLabel`
- All icons MUST use `<Icon>` component
- All standalone buttons MUST use `<Button>` component
- Cards for content containers MUST use `<Card>` component

## New Component Scaffolding

When creating new components, use the following structure:

### `.razor` file (`src/Feirb.Web/Components/{ComponentName}.razor`)

```razor
@namespace Feirb.Web.Components
@inject IStringLocalizer<SharedResources> L

@* Use semantic HTML elements — not just <div> soup *@
<section class="container" aria-label="@L["{ComponentName}Section"]">
    @* Component markup using Bootstrap 5 classes *@
</section>
```

### Code-behind (`.razor.cs`)

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

### Component design patterns

- Component parameters use `[Parameter]` attribute
- Event callbacks use `EventCallback<T>`
- Use `RenderFragment` parameters for flexible content projection
- Component parameters should have sensible defaults where possible
- Use code-behind (`.razor.cs`) for any non-trivial logic
- If the component needs API data, create or reuse a typed HttpClient service in `src/Feirb.Web/Services/`

### UX states (must be handled for every component)

- **Loading states:** show spinners or skeleton screens while data loads, use `aria-busy="true"` on loading containers
- **Error states:** display user-friendly messages with retry options, not raw exceptions
- **Empty states:** show a helpful message when lists/data are empty, not just a blank area
- **Form validation:** use inline validation feedback next to the relevant field, not just top-level alerts

## Conventions

- Follow all conventions from CLAUDE.md (file-scoped namespaces, primary constructors, etc.)
- All user-facing strings through `IStringLocalizer<SharedResources>`
- Add `.resx` keys to all locale files when adding new strings
- Scoped CSS (`.razor.css`) for page-specific layout; global `app.css` for primitive styles only

## Verification

1. **Build and test:**
   ```bash
   dotnet build Feirb.sln
   dotnet test Feirb.sln --verbosity normal
   dotnet format Feirb.sln --verify-no-changes
   ```

2. **Create Playwright tests** using `/implement-playwright-tests`:
   - Test new or changed pages/routes for correct rendering and interaction
   - If new primitives were created, add them to the component showcase and test them there

3. **Manual verification** using `/dev-harness`:
   - Start the app and visually verify the UI changes
   - Check responsive behavior at different breakpoints
   - Verify accessibility (keyboard navigation, screen reader)

## After Implementation

- List which primitives were used
- List any new primitives created
- List any `.resx` keys added
- List any Playwright tests created
- Note any migration opportunities spotted (existing pages using raw Bootstrap for covered elements)
