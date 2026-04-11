# Feirb Component Library

Shared UI primitives in `src/Feirb.Web/Components/UI/` and higher-level layout components in `src/Feirb.Web/Components/`.
Global styles in `src/Feirb.Web/wwwroot/css/app.css` (prefixed `feirb-`).

**Every component listed here has a matching showcase page** at `/components-showcase/<name>` — use it to see the component rendered with interactive parameter controls.

**If you need a component and it's not in this file**, grep `src/Feirb.Web/Components/` before concluding it doesn't exist. The registry is authoritative for documented primitives but may lag behind new additions. If you find an undocumented component, add it here as part of your change.

## Icon

Renders a Bootstrap Icon with consistent sizing.

```razor
<Icon Name="gear" />
<Icon Name="pencil" Size="IconSize.Small" />
<Icon Name="envelope" Size="IconSize.Large" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Name` | `string` (required) | — | Icon name without `bi-` prefix |
| `Size` | `IconSize` | `Default` | `Small` (0.875rem), `Default` (1.25rem), `Large` (2rem) |
| `Class` | `string?` | `null` | Extra CSS classes |

## Heading

Semantic heading with optional icon and subtitle.

```razor
<Heading Level="HeadingLevel.Large" Icon="gear">Settings</Heading>
<Heading Level="HeadingLevel.Medium" Subtitle="Configure your account">Profile</Heading>
<Heading Level="HeadingLevel.Small">Section Title</Heading>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Level` | `HeadingLevel` | `Large` | `Large` (h1), `Medium` (h2), `Small` (h3) |
| `Icon` | `string?` | `null` | Icon name (without `bi-` prefix) |
| `Subtitle` | `string?` | `null` | Subtitle text below the heading |
| `ChildContent` | `RenderFragment` (required) | — | Heading text/content |
| `Class` | `string?` | `null` | Extra CSS classes |

## Button

Standard button with optional icon.

```razor
<Button Variant="ButtonVariant.Primary" OnClick="Save">Save</Button>
<Button Variant="ButtonVariant.Danger" Size="ButtonSize.Small" Icon="trash">Delete</Button>
<Button Icon="download" IconPosition="IconPosition.Right">Export</Button>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Variant` | `ButtonVariant` | `Primary` | `Primary`, `Secondary`, `Danger`, `Warning` |
| `Size` | `ButtonSize` | `Medium` | `Small`, `Medium`, `Large` |
| `Icon` | `string?` | `null` | Icon name (without `bi-` prefix) |
| `IconPosition` | `IconPosition` | `Left` | `Left` or `Right` |
| `ChildContent` | `RenderFragment?` | — | Button text/content |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Click handler |
| `Disabled` | `bool` | `false` | Disabled state |
| `Type` | `string` | `"button"` | HTML button type |
| `Title` | `string?` | `null` | Tooltip |
| `Class` | `string?` | `null` | Extra CSS classes |

## CircularButton

Icon-only circular button. Requires `AriaLabel` for WCAG AA.

```razor
<CircularButton AriaLabel="Settings" Variant="ButtonVariant.Primary" OnClick="OpenSettings">
    <Icon Name="gear" />
</CircularButton>
<CircularButton AriaLabel="Delete" Variant="ButtonVariant.Danger" Size="ButtonSize.Small" OnClick="Remove">
    <Icon Name="x" Size="IconSize.Small" />
</CircularButton>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `AriaLabel` | `string` (required) | — | Accessible name |
| `Variant` | `ButtonVariant` | `Primary` | Visual variant |
| `Size` | `ButtonSize` | `Medium` | `Small` (2rem), `Medium` (2.5rem), `Large` (3rem) |
| `ChildContent` | `RenderFragment?` | — | Typically an `<Icon>` |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Click handler |
| `Disabled` | `bool` | `false` | Disabled state |
| `Title` | `string?` | `null` | Tooltip (falls back to AriaLabel) |
| `Class` | `string?` | `null` | Extra CSS classes |

## Card

Minimal card container.

```razor
<Card>
    <h3>Title</h3>
    <p>Content goes here.</p>
</Card>
<Card Class="p-4">
    <p>Card with extra padding.</p>
</Card>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment` (required) | — | Card content |
| `Class` | `string?` | `null` | Extra CSS classes |

## NavigationItem

Card-style navigation link with icon, title, optional subtitle, and chevron. Located in `src/Feirb.Web/Components/`.

```razor
<NavigationItem Icon="person" Title="Personal Information"
    Subtitle="Update your name and email address"
    Href="/settings/personal-information" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Icon` | `string` (required) | — | Icon name without `bi-` prefix |
| `Title` | `string` (required) | — | Navigation item title |
| `Subtitle` | `string?` | `null` | Description text below the title |
| `Href` | `string` (required) | — | Target URL |

**Primitives used:** `Icon` (for icon and chevron)

## ContentSection

Card with header (icon + heading) and body for child content. Located in `src/Feirb.Web/Components/`.

```razor
<ContentSection Icon="translate" Title="Language"
    Subtitle="Choose your preferred display language">
    <p>Content goes here.</p>
</ContentSection>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Icon` | `string` (required) | — | Icon name without `bi-` prefix |
| `Title` | `string` (required) | — | Section title |
| `Subtitle` | `string?` | `null` | Subtitle text |
| `ChildContent` | `RenderFragment?` | — | Section body content |
| `Class` | `string?` | `null` | Extra CSS classes |

**Primitives used:** `Icon` (for header icon), `Heading` (for title/subtitle)

## InfoHeader

Page header with optional icon, subtitle, and toggleable help section. Located in `src/Feirb.Web/Components/`.

```razor
<InfoHeader Title="Settings" Icon="gear" Subtitle="Manage your account and preferences" />

<InfoHeader Title="Mailboxes" Icon="envelope" Subtitle="Connect your email accounts">
    <HelpContent>
        Here you can add IMAP/SMTP mailboxes and test connectivity.
    </HelpContent>
</InfoHeader>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` (required) | — | Header title text |
| `Level` | `HeadingLevel` | `Large` | `Large` (h1), `Medium` (h2), `Small` (h3) |
| `Icon` | `string?` | `null` | Icon name without `bi-` prefix |
| `Subtitle` | `string?` | `null` | Subtitle text below the title |
| `HelpContent` | `RenderFragment?` | `null` | Collapsible help content (shows toggle button when set) |
| `Class` | `string?` | `null` | Extra CSS classes |

**Primitives used:** `Heading` (for title/subtitle), `Icon` (for help icon), `CircularButton` (for toggle)

## SidebarMenu

Data-driven sidebar navigation with optional section headers and a bottom-pinned slot. Uses `NavLink` internally for active state tracking.

```razor
<SidebarMenu Sections="_sections" BottomItems="_bottomItems" />

@code {
    private IReadOnlyList<SidebarMenuSection> _sections =
    [
        new([new("speedometer2", "Dashboard", Href: "")]),
        new(
        [
            new("inbox", "Inbox", Href: "mail/inbox"),
            new("send", "Sent", Href: "mail/sent")
        ], Label: "Main")
    ];

    private IReadOnlyList<SidebarMenuItem> _bottomItems =
    [
        new("gear", "Settings", Href: "settings"),
        new("box-arrow-right", "Logout", OnClick: LogoutAsync)
    ];
}
```

### SidebarMenu Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Sections` | `IReadOnlyList<SidebarMenuSection>` (required) | — | Menu sections for the main scrollable area |
| `BottomItems` | `IReadOnlyList<SidebarMenuItem>?` | `null` | Items pinned to the bottom |
| `AriaLabel` | `string` | `"Sidebar navigation"` | Accessible label for the nav element |
| `Class` | `string?` | `null` | Extra CSS classes |

### SidebarMenuItem Record

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Icon` | `string` (required) | — | Bootstrap Icon name without `bi-` prefix |
| `Label` | `string` (required) | — | Display text |
| `Href` | `string?` | `null` | Navigation target (renders via `NavLink`) |
| `OnClick` | `Func<Task>?` | `null` | Action callback (renders as `<button>` when set) |
| `Visible` | `Func<bool>?` | `null` | Controls visibility; defaults to always visible |

### SidebarMenuSection Record

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Items` | `IReadOnlyList<SidebarMenuItem>` (required) | — | Menu items in this section |
| `Label` | `string?` | `null` | Optional section header text |

**Primitives used:** `Icon` (for item icons)

## ToggleButtonGroup

Mutually exclusive toggle buttons with radio group semantics. Exactly one item is selected at a time.

```razor
<ToggleButtonGroup Items="_themes" @bind-SelectedId="_selectedTheme" />

@code {
    private string _selectedTheme = "emerald";

    private static readonly IReadOnlyList<ToggleButtonItem> _themes =
    [
        new("emerald", "Emerald Grove"),
        new("ocean", "Ocean Breeze"),
        new("sunset", "Sunset Glow")
    ];
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Items` | `IReadOnlyList<ToggleButtonItem>` (required) | — | Available options |
| `SelectedId` | `string` (required) | — | Currently selected item ID |
| `SelectedIdChanged` | `EventCallback<string>` | — | Fires when selection changes |
| `MaxPerRow` | `int` | `8` | Maximum items per row before wrapping |
| `Class` | `string?` | `null` | Extra CSS classes |

### ToggleButtonItem Record

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Label` | `string` | Display text |

**Accessibility:** `role="radiogroup"` on container, `role="radio"` + `aria-checked` on each button. Arrow keys move selection, Home/End jump to first/last.

## LabelPill

Tag-shaped badge for displaying user-created labels. Has a pointed right edge (arrow) and a rounded left edge. Text color (white or black) is automatically determined by WCAG relative luminance of the background color.

```razor
<LabelPill Name="Important" Color="#b6004f" />
<LabelPill Name="Work" Color="#1D76DB" Size="LabelPillSize.Small" />
<LabelPill Name="Personal" Color="#0E8A16" Size="LabelPillSize.Large" />
<LabelPill Name="Uncategorized" />  @* Falls back to black background *@
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Name` | `string` (required) | — | Label display name |
| `Color` | `string?` | `null` | Hex color for background (e.g. `#b6004f`). Falls back to `#000000` |
| `Size` | `LabelPillSize` | `Medium` | `Small`, `Medium`, `Large` |
| `Class` | `string?` | `null` | Extra CSS classes |

Long label names are truncated with ellipsis at `max-width: 12rem`. The `title` attribute shows the full name on hover.

## MailCard

Card component for displaying mail items across different layout contexts. Whole card is a clickable `<a>` navigation target. Located in `src/Feirb.Web/Components/`.

```razor
<MailCard Mode="MailCardMode.Medium"
          SenderName="Alice Johnson"
          SenderEmail="alice@example.com"
          Subject="Q1 Report"
          Summary="Revenue exceeded expectations across all regions."
          Labels="@_labels"
          ReceivedAt="@_receivedAt"
          IsRead="false"
          Href="/mail/123" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Mode` | `MailCardMode` (required) | — | `Medium`, `Small`, `Row` |
| `SenderName` | `string` (required) | — | Sender display name |
| `SenderEmail` | `string` (required) | — | Sender email address |
| `Subject` | `string` (required) | — | Subject line (truncated with ellipsis) |
| `Summary` | `string?` | `null` | Summary text (Medium: 2 lines, hidden otherwise) |
| `Labels` | `IReadOnlyList<MailCardLabel>` | `[]` | Labels to display with overflow "+N" |
| `ReceivedAt` | `DateTime` (required) | — | When the mail was received |
| `IsRead` | `bool` | `false` | Read/unread state |
| `Href` | `string` (required) | — | Navigation target URL |
| `Class` | `string?` | `null` | Extra CSS classes |

### Mode matrix

| Element | Medium | Small | Row |
|---------|--------|-------|-----|
| Avatar | Full (3rem) | Reduced (2.5rem) | Mini (2rem) |
| Name + Email | Yes | Yes | Yes |
| Subject (truncated) | Yes | Yes | Yes |
| Summary | 2 lines | Hidden | Hidden |
| Labels | 3 (medium) | 1 (small) | 4 (medium) |
| Date/Time | Stacked | Date only | Inline |
| Read icon | Bottom-right | Bottom-right | Far-left |

### MailCardLabel Record

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Label display name |
| `Color` | `string?` | Hex color for background (falls back to black) |

**Primitives used:** `Card` (implicit via styling), `Icon` (avatar, read indicator), `LabelPill` (labels)

## Table

Grid-based table with ARIA semantics. Children: `TableHeader` + `TableBody`. Columns defined via `TableColumn`; row cells via `TableCell`. Column widths cascade to the grid via `TableColumn.Width`.

```razor
<Table Hover>
    <TableHeader>
        <TableColumn>Name</TableColumn>
        <TableColumn>Email</TableColumn>
        <TableColumn Width="8rem">Created</TableColumn>
        <TableColumn Width="6rem">Actions</TableColumn>
    </TableHeader>
    <TableBody>
        @foreach (var user in _users)
        {
            <TableRow OnClick="() => NavigateToDetail(user.Id)" data-testid="user-row">
                <TableCell>@user.Name</TableCell>
                <TableCell>@user.Email</TableCell>
                <TableCell>@user.CreatedAt.ToString("yyyy-MM-dd")</TableCell>
                <TableCell>
                    <Button Variant="ButtonVariant.Danger" Size="ButtonSize.Small" Icon="trash" OnClick="() => Delete(user)" />
                </TableCell>
            </TableRow>
        }
    </TableBody>
</Table>
```

### Table Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment` (required) | — | `TableHeader` + `TableBody` |
| `Size` | `TableSize` | `Default` | `Default` or `Small` |
| `Hover` | `bool` | `false` | Highlight row on hover |
| `Striped` | `bool` | `false` | Alternating row background |
| `Class` | `string?` | `null` | Extra CSS classes |

### TableColumn Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | — | Column header text |
| `Width` | `string` | `"1fr"` | CSS grid column width (e.g. `"8rem"`, `"2fr"`) |

### TableRow Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment` (required) | — | `TableCell` elements |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | When set, row gets `clickable` class and cursor; use for row-level navigation |
| `Class` | `string?` | `null` | Extra CSS classes |

**Clickable rows:** If `OnClick` is set, the row automatically becomes keyboard-focusable and the `clickable` class is applied. Don't add explicit `Clickable` parameter — it doesn't exist.

## Pagination

Theme-aware previous/next pager with page indicator and optional page size selector.

```razor
<Pagination CurrentPage="_page"
            TotalPages="_totalPages"
            CurrentPageChanged="OnPageChangedAsync"
            PreviousLabel="@L["PaginationPrevious"]"
            NextLabel="@L["PaginationNext"]" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CurrentPage` | `int` (required) | — | 1-based current page |
| `TotalPages` | `int` (required) | — | Total number of pages |
| `CurrentPageChanged` | `EventCallback<int>` (required) | — | Fires when page changes |
| `PageSize` | `int` | `5` | Current page size |
| `PageSizeChanged` | `EventCallback<int>` | — | When set, page size selector is shown |
| `PageSizeOptions` | `int[]` | `[5, 10, 25, 50, 100]` | Sizes offered in the selector |
| `PreviousLabel` | `string` | `"Previous"` | Always pass the localized `L["PaginationPrevious"]` |
| `NextLabel` | `string` | `"Next"` | Always pass the localized `L["PaginationNext"]` |
| `PerPageLabel` | `string` | `"per page"` | Label shown next to size selector |
| `Sticky` | `bool` | `false` | Stick to bottom of viewport |
| `Class` | `string?` | `null` | Extra CSS classes |

The component renders nothing if `TotalPages <= 1` and no size selector is requested.

## PersonChip

Compact person display with avatar, name, optional email, and optional status badge. Located in `src/Feirb.Web/Components/UI/`.

```razor
<PersonChip Name="Alice Johnson" Email="alice@example.com" />
<PersonChip Name="Bob Smith" Email="bob@example.com" Size="PersonChipSize.Small" Status="RecipientStatus.Important" />
<PersonChip Name="Unknown Sender" Email="someone@external.com" Size="PersonChipSize.Mini" Status="RecipientStatus.Unknown" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Name` | `string` (required) | — | Display name |
| `Email` | `string?` | `null` | Email address (hidden in Mini size) |
| `Size` | `PersonChipSize` | `Default` | `Default`, `Small`, `Mini` |
| `Status` | `RecipientStatus` | `None` | `None` (no badge), `Known`, `Unknown`, `Important`, `Blocked` |
| `StatusTitle` | `string?` | `null` | Tooltip for the status badge |
| `Class` | `string?` | `null` | Extra CSS classes |

**Avatar lookup:** If `Email` is set, the component fetches `/api/avatars/{hash}` automatically. The endpoint returns `204` when no avatar exists — the component falls back to a dashed placeholder icon.

**Status badge colors:** `Known` → primary, `Unknown` → secondary, `Important` → warning, `Blocked` → danger. `None` renders no badge (use for chips outside address-book context like Compose recipient tokens).

## Toggle

Switch-style boolean input with optional separate label for the off state.

```razor
<Toggle Text="@L["CcBccVisible"]" DisabledText="@L["CcBccHidden"]" @bind-Value="_showCcBcc" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Value` | `bool` | `false` | Current state |
| `ValueChanged` | `EventCallback<bool>` | — | Use `@bind-Value` |
| `Text` | `string` (required) | — | Label when on (also used when off unless `DisabledText` is set) |
| `DisabledText` | `string?` | `null` | Label when off; if null, `Text` is shown strikethrough |
| `For` | `Expression<Func<bool>>?` | `null` | For EditContext integration in forms |
| `Class` | `string?` | `null` | Extra CSS classes |

## StatusMessage

Centered empty/error state display with icon, title, message, and optional action link.

```razor
<StatusMessage Icon="inbox"
               Title="@L["NoMessages"]"
               Message="@L["NoMessagesDescription"]"
               ButtonText="@L["ComposeNew"]"
               ButtonHref="/compose" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Icon` | `string` (required) | — | Bootstrap Icon name without `bi-` prefix |
| `Title` | `string` (required) | — | Heading text |
| `Message` | `string` (required) | — | Descriptive message below the heading |
| `ButtonText` | `string?` | `null` | Optional action button label |
| `ButtonHref` | `string?` | `null` | Action button target (required if `ButtonText` is set) |
| `IsFullPage` | `bool` | `false` | Render title as `h1` (for full-page errors like 404) |

Use for empty lists, 404 pages, and loading failures. For inline errors inside forms, use Bootstrap alert classes or `ValidationMessage`.

## RecipientInput

Tokenizing input for email recipients with PersonChip tokens, paste-splitting, and autocomplete. Used in Compose.

```razor
<RecipientInput @bind-Recipients="_to"
                Id="compose-to"
                Placeholder="@L["ComposePlaceholderTo"]"
                DataTestId="compose-to"
                ContactsProvider="SearchRecipientsAsync" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Recipients` | `List<RecipientEntry>` (required) | `[]` | Current tokenized recipients; use `@bind-Recipients` |
| `RecipientsChanged` | `EventCallback<List<RecipientEntry>>` | — | Use `@bind-Recipients` |
| `Contacts` | `IReadOnlyList<RecipientEntry>` | `[]` | Static suggestion list (fallback if no provider) |
| `ContactsProvider` | `Func<string, Task<IReadOnlyList<RecipientEntry>>>?` | `null` | Async query→results provider; takes precedence over `Contacts` |
| `Placeholder` | `string?` | `null` | Placeholder when no recipients |
| `Id` | `string?` | `null` | HTML id (enables `<label for=...>`) |
| `DataTestId` | `string?` | `null` | Test ID on the inner input |
| `Class` | `string?` | `null` | Extra CSS classes (use `is-invalid` for error state) |

**Confirmation triggers:** Enter, Tab, comma, semicolon, space, or blur. Paste splits on comma/semicolon/newline. Duplicates flash the existing chip.

## Breadcrumb

Auto-segmented navigation trail based on the current URL. Rendered by the layout, **do not include it manually on pages**. Located in `src/Feirb.Web/Components/`.

### Label map

Path segments are resolved to labels via a `switch` in `Breadcrumb.razor` (`ResolveLabel` method). **When you add a new page**, add a case for every intermediate URL segment:

```csharp
private string ResolveLabel(string fullPath, string segment) =>
    fullPath switch
    {
        "address-book" => L["AddressBookNavLabel"],
        "address-book/contacts" => L["AddressBookContactsHeading"],
        "address-book/addresses" => L["AddressBookAddressesHeading"],
        // ...
        _ => segment
    };
```

### Href map

Intermediate segments without their own landing page must point somewhere valid. Override in `ResolveHref`:

```csharp
private static string ResolveHref(string fullPath) =>
    fullPath switch
    {
        "address-book/contacts" => "/address-book",   // no listing page at /address-book/contacts
        "address-book/addresses" => "/address-book",
        _ => $"/{fullPath}"
    };
```

Otherwise the breadcrumb link will 404 when clicked.

### BreadcrumbOverrideService

For detail pages, the last URL segment is typically a GUID. Inject `BreadcrumbOverrideService` and set a human-readable label in `OnInitializedAsync` / after data loads:

```csharp
@inject BreadcrumbOverrideService BreadcrumbOverride
@implements IDisposable

protected override async Task OnInitializedAsync()
{
    var entity = await Http.GetFromJsonAsync<Entity>($"/api/entities/{Id}");
    BreadcrumbOverride.SetLastSegmentLabel(entity.DisplayName);
}

public void Dispose() => BreadcrumbOverride.Clear();
```

Always clear the override in `Dispose` so it doesn't leak to the next page.

## Toolbar

Page-level action bar rendered by the layout (`src/Feirb.Web/Components/Toolbar.razor`). Pages don't render the Toolbar themselves — they register actions via `ToolbarStateService`, and the layout renders them.

### ToolbarStateService

Scoped service. Inject in any page that needs toolbar actions:

```csharp
@inject ToolbarStateService ToolbarState
@implements IDisposable

@code {
    private ToolbarAction[] _toolbarActions = [];

    protected override void OnInitialized()
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
    }

    public void Dispose() => ToolbarState.RemoveActions(_toolbarActions);
}
```

### ToolbarAction

| Field | Type | Description |
|-------|------|-------------|
| `Label` | `string` | Button label (always localized) |
| `Variant` | `ButtonVariant` | Visual variant — see conventions below |
| `OnClickAsync` | `Func<Task>` | Async click handler |
| `Icon` | `string?` | Bootstrap Icon name without `bi-` prefix |

### Variant conventions on detail pages

| Action | Variant | Icon |
|--------|---------|------|
| **Cancel** | `Warning` | `x-lg` |
| **Save** | `Primary` | `check-lg` |
| **Delete** | `Danger` | `trash` |
| **Create/Add** | `Primary` | `plus-lg` |
| **Secondary actions** (e.g. "Add to contacts") | `Secondary` | context-specific |

**Never use a plain "Back" button in the toolbar on form pages** — the breadcrumb already provides navigation, and Cancel carries the additional "discard unsaved edits" semantic.

### Conditional actions

If an action depends on entity state, rebuild the array at the end of your data-load method:

```csharp
private void RebuildToolbar()
{
    ToolbarState.RemoveActions(_toolbarActions);
    var actions = new List<ToolbarAction> { /* always-present actions */ };
    if (_entity.NeedsExtraAction)
        actions.Add(new ToolbarAction(...));
    _toolbarActions = [.. actions];
    ToolbarState.AddActions(_toolbarActions);
}
```

## TrackedEditForm

Form wrapper that integrates with `UnsavedChangesService` for dirty tracking and toolbar-driven save. Use this **instead of raw `<EditForm>`** on any detail/edit page. Located in `src/Feirb.Web/Components/`.

```razor
@inject UnsavedChangesService UnsavedChanges

<ContentSection Icon="person-gear" Title="@L["ContactDetailPageTitle"]">
    <TrackedEditForm Model="_model" OnSave="HandleSaveAsync">
        <DataAnnotationsValidator />
        <div class="mb-3">
            <label for="displayName" class="form-label">@L["DisplayName"]</label>
            <InputText id="displayName" class="form-control" @bind-Value="_model.DisplayName" />
            <ValidationMessage For="() => _model.DisplayName" />
        </div>
    </TrackedEditForm>
</ContentSection>

@code {
    private async Task<bool> HandleSaveAsync()
    {
        var response = await Http.PutAsJsonAsync($"/api/entities/{Id}", _model);
        if (response.IsSuccessStatusCode)
        {
            Notifications.Add(L["ButtonSave"], NotificationSeverity.Success);
            return true;   // marks form clean
        }
        Notifications.Add("Save failed", NotificationSeverity.Error);
        return false;
    }
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Model` | `object` (required) | — | The form model to bind to |
| `ChildContent` | `RenderFragment?` | — | Form fields + validator |
| `OnSave` | `Func<Task<bool>>` (required) | — | Save handler; return `true` to mark clean |

**How it works:**
- Registers with `UnsavedChangesService` on init
- Tracks `EditContext.OnFieldChanged` to flip the dirty bit
- `SubmitAsync()` (called internally by `UnsavedChanges.SaveAllAsync()`) validates then invokes `OnSave`
- Clean state is restored automatically when `OnSave` returns `true`

### UnsavedChangesService

The service is scoped and auto-registers on the layout. You only interact with it from toolbar actions:

```csharp
new ToolbarAction(L["ButtonSave"], ButtonVariant.Primary,
    async () => { await UnsavedChanges.SaveAllAsync(); }, "check-lg"),
```

`SaveAllAsync()` calls `SubmitAsync()` on every registered `TrackedEditForm` — almost always exactly one per page.

## NotificationService

Scoped service for user-facing toast notifications. Four severities with auto-dismiss timings.

```csharp
@inject NotificationService Notifications

Notifications.Add(L["SuccessSaved"], NotificationSeverity.Success);
Notifications.Add("Failed to save", NotificationSeverity.Error);
```

| Severity | Auto-dismiss | Usage |
|----------|-------------|-------|
| `Success` | 10s | Successful mutation (save, create, delete) |
| `Info` | 10s | Neutral info ("Sync started") |
| `Warning` | 30s | Recoverable problem ("Retry suggested") |
| `Error` | never | Failed mutation or unexpected error — user must dismiss |

The layout renders active notifications via `ToastContainer`. Don't render toasts yourself.

**When to use:**
- CRUD mutation outcome (success + failure both get a toast)
- Long-running background task completion
- Network failures on page load

**When NOT to use:**
- Form validation errors → use `<ValidationMessage>` inline
- Page state changes triggered directly by user click (click usually provides its own feedback)

## Enums

Defined in `UIEnums.cs`:

- **`IconSize`**: `Small`, `Default`, `Large`
- **`ButtonVariant`**: `Primary`, `Secondary`, `Danger`, `Warning`
- **`ButtonSize`**: `Small`, `Medium`, `Large`
- **`IconPosition`**: `Left`, `Right`
- **`HeadingLevel`**: `Large`, `Medium`, `Small`
- **`LabelPillSize`**: `Small`, `Medium`, `Large`
- **`MailCardMode`**: `Medium`, `Small`, `Row`

## ColorPicker

Color picker with 12-preset ring, hex input, and confirm/cancel dialog. Trigger displays a colored swatch and a "Choose color" button.

```razor
<ColorPicker @bind-Value="_color" />

@code {
    private string? _color = "#FF0000";
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Value` | `string?` | `null` | Current hex color value (e.g. `#FF0000`) |
| `ValueChanged` | `EventCallback<string?>` | — | Fires when color is confirmed |

**Presets:** Red, Vermilion, Orange, Amber, Yellow, Chartreuse, Green, Teal, Blue, Violet, Purple, Magenta

**Primitives used:** `Button` (trigger and dialog buttons), `Icon` (via Button)

## Design Tokens

All primitives use CSS custom properties from `app.css`. Key tokens:

| Token | Value | Usage |
|-------|-------|-------|
| `--bs-primary` | `#b6004f` | Primary actions |
| `--feirb-on-surface` | `#2d2d43` | Secondary actions, text |
| `--feirb-error` | `#b31b25` | Danger actions |
| `--bs-warning` | `#FFE04A` | Warning actions |
| `--feirb-shadow` | `0 8px 24px rgba(0,0,0,0.06)` | Card shadow |
| `--bs-border-radius` | `1rem` | Card corners |
