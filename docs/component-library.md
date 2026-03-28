# Feirb Component Library

Shared UI primitives in `src/Feirb.Web/Components/UI/`.
Global styles in `src/Feirb.Web/wwwroot/css/app.css` (prefixed `feirb-`).

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

## Enums

Defined in `UIEnums.cs`:

- **`IconSize`**: `Small`, `Default`, `Large`
- **`ButtonVariant`**: `Primary`, `Secondary`, `Danger`, `Warning`
- **`ButtonSize`**: `Small`, `Medium`, `Large`
- **`IconPosition`**: `Left`, `Right`
- **`HeadingLevel`**: `Large`, `Medium`, `Small`

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
