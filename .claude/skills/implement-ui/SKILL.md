---
name: implement-ui
description: Implement UI work using the shared component library with consistent styling enforcement
user_invocable: true
args: description_of_ui_work
---

# Implement UI

Implement UI work using registered primitives from the component library. Enforces consistent styling across all pages.

## Steps

1. **Read the component registry:** `docs/component-library.md`. This is authoritative for documented primitives.

2. **Grep `src/Feirb.Web/Components/` if the registry is silent** — the registry may lag behind new additions. Never conclude "it doesn't exist" from the registry alone. If you find an undocumented component you end up using, add a registry entry for it as part of your change.

3. **Check the showcase pages** (`src/Feirb.Web/Pages/Showcase/Showcase*.razor`) to see components rendered interactively with parameter controls. Reuse or extend existing components when possible; only reach for Bootstrap classes when nothing in the library fits.

4. **Read the page/component context** — understand what exists, what needs to change.

5. **Draft a short plan** for the UI work:
   - Which primitives will be used
   - Which elements need to change
   - Any new primitives needed
   - Ask the user to confirm before proceeding

6. **Implement using registered primitives:**
   - Replace raw Bootstrap button classes with `<Button>` or `<CircularButton>`
   - Replace raw `<i class="bi bi-*">` with `<Icon>` **when the icon is registered in the `Icon` component**. If the icon isn't registered, raw `<i class="bi bi-...">` is acceptable — OR add the icon to the `Icon` component's registered set. Don't silently drop icons the project needs.
   - Replace raw card markup with `<Card>`
   - Follow existing page patterns for layout and structure — see "Anatomy of a list page" and "Anatomy of a detail page" below

7. **WCAG AA check:**
   - All interactive elements are keyboard-focusable
   - `aria-label` on icon-only buttons (CircularButton enforces this)
   - Contrast meets 4.5:1 for text, 3:1 for large text and UI components
   - Semantic HTML elements where appropriate
   - No color-only indicators

8. **Responsive check:**
   - Desktop primary, mobile supported
   - Use Bootstrap breakpoints (`col-sm-`, `col-md-`, `col-lg-`) for layout
   - Test that content doesn't overflow on small screens

9. **If a small primitive is missing** (e.g., a Badge, Divider, or similar):
   - Pause implementation
   - Propose the primitive with 2-3 design questions:
     - What variants does it need?
     - What sizes?
     - Any accessibility requirements?
   - After user confirms, create it in `src/Feirb.Web/Components/UI/`
   - Add styles to `app.css` with `feirb-` prefix
   - **Add a showcase page** at `src/Feirb.Web/Pages/Showcase/Showcase{Name}.razor` (see "Showcase requirement" below)
   - **Update `docs/component-library.md`** with a full registry entry
   - Continue implementation

10. **If a large primitive is missing** (e.g., Modal, DataTable, Toolbar redesign):
    - Propose a GitHub issue for the primitive
    - User decides: create issue and defer, or build it now
    - If building now, follow the same pattern as step 9 but with full design exploration

11. **If UX exploration is needed** (interaction patterns, layout decisions, responsive behavior):
    - Suggest using `/designer-grill-me` for design exploration
    - Wait for design decisions before implementing

## Hard Rules

- **If a primitive exists, use it** — never emit raw Bootstrap classes for elements covered by the library
- Non-covered elements (modals, forms, tables, menus): use Bootstrap classes but encourage componentization
- If a non-covered pattern appears 2+ times, create a refactoring issue
- All icon-only buttons MUST use `<CircularButton>` with `AriaLabel`
- All icons SHOULD use `<Icon>` component — raw `<i class="bi bi-...">` is acceptable ONLY when the icon isn't in the Icon component's registered set. Prefer adding the icon to the `Icon` component if it's used more than once.
- All standalone buttons MUST use `<Button>` component
- Cards for content containers MUST use `<Card>` component
- **Every new shared component MUST have a showcase page AND a registry entry** (see "Showcase requirement" below)
- **Repeated caller ritual = bake it in.** If the same class/prop/wrapper appears at 3+ call sites of a component (e.g. `Class="mb-4"` on every `<ContentSection>`), move it into the component itself. Callers shouldn't have to remember defaults.

## Showcase requirement

Every shared component in `src/Feirb.Web/Components/` (top-level or `UI/`) must have a matching showcase page at `src/Feirb.Web/Pages/Showcase/Showcase{Name}.razor`. The showcase should:

1. **Summary card** — one-sentence description + parameter table (name / type / default / description)
2. **Preview card** — interactive controls (inputs/selects) that map to parameters, with the component rendered below
3. **Variant/state cards** — one per distinct visual state (e.g. all sizes, all status badges, empty vs loaded)
4. **Code card** — live-updating code snippet reflecting the current preview settings

When adding a new component, create the showcase page in the same commit and link it from `docs/component-library.md`. The showcase is the canonical place for humans to explore the component; the registry is the canonical place for LLMs to look up its API.

Services without visual components (e.g. `NotificationService`, `BreadcrumbOverrideService`) don't need a showcase — but they still need a registry entry.

## Anatomy of a list page

Recurring shape for any "list of entities with detail subpages" page (Labels, Mailboxes, Admin Users, Address Book). Use this as the starting skeleton.

```razor
@page "/entities"
@attribute [Authorize]
@using Feirb.Shared.Entities
@using Feirb.Web.Components.UI
@inject HttpClient Http
@inject IStringLocalizer<SharedResources> L
@inject NavigationManager Navigation
@inject ToolbarStateService ToolbarState
@implements IDisposable

<PageTitle>@L["EntitiesPageTitle"]</PageTitle>

<InfoHeader Title="@L["EntitiesPageTitle"]" Icon="collection" Subtitle="@L["EntitiesPageSubtitle"]" />

<ContentSection Icon="list-ul" Title="@L["EntitiesHeading"]" Subtitle="@L["EntitiesSubtitle"]">
    @if (_loading)
    {
        <div class="text-center py-4"><div class="spinner-border spinner-border-sm" role="status"></div></div>
    }
    else if (_page is null || _page.Items.Count == 0)
    {
        <StatusMessage Icon="inbox" Title="@L["NoEntitiesTitle"]" Message="@L["NoEntitiesMessage"]" />
    }
    else
    {
        <Table Hover>
            <TableHeader>
                <TableColumn>@L["ColumnName"]</TableColumn>
                <TableColumn Width="10rem">@L["ColumnUpdated"]</TableColumn>
            </TableHeader>
            <TableBody>
                @foreach (var item in _page.Items)
                {
                    <TableRow OnClick="() => Navigation.NavigateTo($"/entities/{item.Id}")" data-testid="entity-row">
                        <TableCell>@item.Name</TableCell>
                        <TableCell>@item.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")</TableCell>
                    </TableRow>
                }
            </TableBody>
        </Table>
        <Pagination CurrentPage="_currentPage" TotalPages="_page.TotalPages"
                    CurrentPageChanged="OnPageChangedAsync"
                    PreviousLabel="@L["PaginationPrevious"]" NextLabel="@L["PaginationNext"]" />
    }
</ContentSection>

@code {
    private const int _pageSize = 25;
    private PagedResult<EntityListItem>? _page;
    private bool _loading = true;
    private int _currentPage = 1;
    private ToolbarAction[] _toolbarActions = [];

    protected override async Task OnInitializedAsync()
    {
        _toolbarActions =
        [
            new ToolbarAction(L["ButtonCreate"], ButtonVariant.Primary,
                () => { Navigation.NavigateTo("/entities/new"); return Task.CompletedTask; }, "plus-lg"),
        ];
        ToolbarState.AddActions(_toolbarActions);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try { _page = await Http.GetFromJsonAsync<PagedResult<EntityListItem>>($"/api/entities?page={_currentPage}&pageSize={_pageSize}"); }
        catch { _page = null; }
        finally { _loading = false; }
    }

    private async Task OnPageChangedAsync(int page) { _currentPage = page; await LoadAsync(); }

    public void Dispose() => ToolbarState.RemoveActions(_toolbarActions);
}
```

## Anatomy of a detail page

Recurring shape for any "edit a single entity" page. Use this as the starting skeleton.

```razor
@page "/entities/{Id:guid}"
@attribute [Authorize]
@using Feirb.Shared.Entities
@using Feirb.Web.Components.UI
@inject HttpClient Http
@inject IStringLocalizer<SharedResources> L
@inject NavigationManager Navigation
@inject NotificationService Notifications
@inject BreadcrumbOverrideService BreadcrumbOverride
@inject ToolbarStateService ToolbarState
@inject UnsavedChangesService UnsavedChanges
@implements IDisposable

<PageTitle>@(_entity?.DisplayName ?? L["EntityDetailPageTitle"])</PageTitle>

<InfoHeader Title="@(_entity?.DisplayName ?? L["EntityDetailPageTitle"])" Icon="person-fill" Subtitle="@L["EntityDetailSubtitle"]" />

@if (_loading)
{
    <div class="text-center py-5"><div class="spinner-border" role="status"></div></div>
}
else if (_entity is null)
{
    <StatusMessage Icon="question-circle" Title="@L["EntityNotFoundTitle"]" Message="@L["EntityNotFoundMessage"]" />
}
else
{
    <ContentSection Icon="pencil" Title="@L["EntityDetailPageTitle"]" Subtitle="@_entity.DisplayName">
        <TrackedEditForm Model="_model" OnSave="HandleSaveAsync">
            <DataAnnotationsValidator />
            <div class="mb-3">
                <label for="entityName" class="form-label">@L["EntityDisplayName"]</label>
                <InputText id="entityName" class="form-control" @bind-Value="_model.DisplayName" data-testid="entity-display-name" />
                <ValidationMessage For="() => _model.DisplayName" />
            </div>
        </TrackedEditForm>
    </ContentSection>
}

@code {
    [Parameter] public Guid Id { get; set; }

    private EntityResponse? _entity;
    private bool _loading = true;
    private FormModel _model = new();
    private ToolbarAction[] _toolbarActions = [];

    protected override async Task OnInitializedAsync()
    {
        _toolbarActions =
        [
            new ToolbarAction(L["ButtonCancel"], ButtonVariant.Warning,
                () => { Navigation.NavigateTo("/entities"); return Task.CompletedTask; }, "x-lg"),
            new ToolbarAction(L["ButtonSave"], ButtonVariant.Primary,
                async () => { await UnsavedChanges.SaveAllAsync(); }, "check-lg"),
            new ToolbarAction(L["ButtonDelete"], ButtonVariant.Danger, HandleDeleteAsync, "trash"),
        ];
        ToolbarState.AddActions(_toolbarActions);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _entity = await Http.GetFromJsonAsync<EntityResponse>($"/api/entities/{Id}");
            if (_entity is not null)
            {
                _model = new FormModel { DisplayName = _entity.DisplayName };
                BreadcrumbOverride.SetLastSegmentLabel(_entity.DisplayName);
            }
        }
        catch { _entity = null; }
        finally { _loading = false; }
    }

    private async Task<bool> HandleSaveAsync()
    {
        var response = await Http.PutAsJsonAsync($"/api/entities/{Id}", new UpdateEntityRequest(_model.DisplayName!));
        if (response.IsSuccessStatusCode)
        {
            Notifications.Add(L["ButtonSave"], NotificationSeverity.Success);
            await LoadAsync();
            return true;
        }
        Notifications.Add("Save failed", NotificationSeverity.Error);
        return false;
    }

    private async Task HandleDeleteAsync()
    {
        var response = await Http.DeleteAsync($"/api/entities/{Id}");
        if (response.IsSuccessStatusCode)
        {
            Notifications.Add(L["ButtonDelete"], NotificationSeverity.Success);
            Navigation.NavigateTo("/entities");
        }
        else
        {
            Notifications.Add("Delete failed", NotificationSeverity.Error);
        }
    }

    public void Dispose()
    {
        ToolbarState.RemoveActions(_toolbarActions);
        BreadcrumbOverride.Clear();
    }

    private sealed class FormModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(256)]
        public string? DisplayName { get; set; }
    }
}
```

## Breadcrumb integration

The `Breadcrumb` component is rendered by the layout — pages do **not** include it manually. It walks the current URL and maps each segment to a label + href.

**When adding a new route, update two maps in `src/Feirb.Web/Components/Breadcrumb.razor`:**

1. **`ResolveLabel`** — add a case for every URL segment (and intermediate segment) your page introduces. Without this, the breadcrumb shows the raw URL segment.

   ```csharp
   "entities" => L["EntitiesNavLabel"],
   "entities/new" => L["EntitiesNew"],
   ```

2. **`ResolveHref`** — if an intermediate URL segment has no landing page, redirect the crumb elsewhere. Without this, clicking the crumb 404s.

   ```csharp
   "entities/subgroup" => "/entities",   // no page at /entities/subgroup
   ```

**For detail pages with GUID segments**, inject `BreadcrumbOverrideService` and call `SetLastSegmentLabel(...)` after loading the entity. Always clear it in `Dispose` (see the detail-page template above).

## Page-level actions — Toolbar pattern

**Page-level actions belong in the Toolbar, not inline in the page.** Inline buttons only for section-scoped actions (e.g. "Unlink" inside a "Linked Contact" section).

For **edit/detail pages** (any page that edits a single entity via a form), register these toolbar actions in `OnInitializedAsync`:

| Action | Variant | Icon | Behavior |
|--------|---------|------|----------|
| **Cancel** | `Warning` | `x-lg` | Navigate back to the listing. `UnsavedChangesService` warns if dirty. |
| **Save** | `Primary` | `check-lg` | Call `UnsavedChanges.SaveAllAsync()` — validates and submits the `TrackedEditForm`. |
| **Delete** | `Danger` | `trash` | Direct delete, no form involvement. |

**Never use a plain "Back" button** on form pages — the breadcrumb already provides navigation, and Cancel carries the additional "discard unsaved edits" semantic.

Standard keys: `ButtonCancel`, `ButtonSave`, `ButtonDelete`. Don't invent locale keys for page-specific wording unless the action is meaningfully different.

### Template

```csharp
@inject ToolbarStateService ToolbarState
@inject UnsavedChangesService UnsavedChanges
@implements IDisposable

@* ... *@
<ContentSection Icon="..." Title="...">
    <TrackedEditForm Model="_model" OnSave="HandleSaveAsync">
        <DataAnnotationsValidator />
        @* form fields *@
    </TrackedEditForm>
</ContentSection>

@code {
    private ToolbarAction[] _toolbarActions = [];

    protected override async Task OnInitializedAsync()
    {
        _toolbarActions =
        [
            new ToolbarAction(L["ButtonCancel"], ButtonVariant.Warning,
                () => { Navigation.NavigateTo("/listing"); return Task.CompletedTask; }, "x-lg"),
            new ToolbarAction(L["ButtonSave"], ButtonVariant.Primary,
                async () => { await UnsavedChanges.SaveAllAsync(); }, "check-lg"),
            new ToolbarAction(L["ButtonDelete"], ButtonVariant.Danger, HandleDeleteAsync, "trash"),
        ];
        ToolbarState.AddActions(_toolbarActions);
        await LoadAsync();
    }

    private async Task<bool> HandleSaveAsync()
    {
        // PUT/POST the form model. Return true on success (marks form clean).
    }

    public void Dispose() => ToolbarState.RemoveActions(_toolbarActions);
}
```

**Conditional toolbar actions:** If an action depends on entity state (e.g. "Add to contacts" only when the entity is orphaned), rebuild `_toolbarActions` at the end of `LoadAsync()` via a `RebuildToolbar()` helper that removes the old array and adds a freshly-constructed one.

**Form dirty tracking:** Always use `<TrackedEditForm>`, not raw `<EditForm>`, on detail pages. It registers with `UnsavedChangesService` automatically — the Cancel button then warns on dirty, and Save is driven by `SaveAllAsync()`.

## New Component Scaffolding

When creating new components, use the following structure:

### `.razor` file (`src/Feirb.Web/Components/UI/{ComponentName}.razor`)

```razor
@namespace Feirb.Web.Components.UI

@* One-line purpose comment describing the component. *@
<div class="feirb-@(_cssName.ToLowerInvariant()) @Class" @attributes="AdditionalAttributes">
    @* Component markup — use existing primitives where possible *@
    @ChildContent
</div>

@code {
    /// <summary>Child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Any extra HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private const string _cssName = "{componentname}";
}
```

### Scoped CSS (`.razor.css`, co-located)

```css
/* ComponentName — one-line purpose */
.feirb-{componentname} {
    /* use design tokens from app.css, not literal colors */
    color: var(--bs-body-color);
    background: var(--feirb-surface);
}
```

### Code-behind (only when needed)

For non-trivial logic, create `{ComponentName}.razor.cs` as a partial class. Simple components with only `[Parameter]` properties can keep code inside `@code { }` — don't split unnecessarily.

```csharp
namespace Feirb.Web.Components.UI;

public partial class {ComponentName} : ComponentBase
{
    // Non-trivial logic here
}
```

**Razor vs code-behind rule of thumb:** if the `@code { }` block is under ~40 lines and has no complex state machines, keep it inline. Split only when the logic becomes hard to read next to the markup.

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

3. **Browser verification** using `/test-ui`:
   - Verify the UI changes visually (screenshots) and interactively (click, fill, toggle)
   - Check responsive behavior at different breakpoints
   - Assert DOM content via accessibility tree
   - Verify backend state after interactions (API, DB)

## After Implementation

- List which primitives were used
- List any new primitives created
- List any `.resx` keys added
- List any Playwright tests created
- Note any migration opportunities spotted (existing pages using raw Bootstrap for covered elements)
