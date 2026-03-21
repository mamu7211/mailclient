# Feirb — UI/UX Design

Modern, soft light UI with subtle color accents and rounded surfaces.

## Colors

| Token        | Hex       | Usage                                              |
|--------------|-----------|----------------------------------------------------|
| `primary`    | `#FF2D78` | Pink accent — primary actions, active states, highlights |
| `secondary`  | `#00FFCC` | Mint/cyan — secondary actions, success states, indicators |
| `tertiary`   | `#FFE04A` | Warm yellow — warnings, tags, small highlights     |
| `neutral`    | `#28283E` | Main text color and dark UI elements               |
| `background` | `#F4F5FB` | Soft light gray with slight purple tint             |

### Color Usage Rules

- Use `primary` for main CTAs and active states
- Use `secondary` / `tertiary` sparingly
- Large surfaces remain neutral/light
- No glow effects — colors are flat
- Max 1–2 accent colors per section

## Typography

| Element        | Font                              | Style                        |
|----------------|-----------------------------------|------------------------------|
| Headlines      | Geometric sans-serif (Inter/Sora) | Large size, medium–bold weight |
| Body           | Inter or similar                  | High readability, medium contrast |
| Labels/UI text | Same family                       | Smaller size, medium weight  |

## Surfaces & Elevation

- **Border radius:** `16px` – `24px`
- **Soft shadow:** `box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06)`
- **Layering order:** Base background → Raised cards → Interactive elements

## Components

### Buttons

| Variant     | Background    | Text    | Border             | Shadow |
|-------------|---------------|---------|--------------------|--------|
| Primary     | `primary`     | white   | none               | soft   |
| Secondary   | light         | neutral | subtle             | none   |
| Outlined    | transparent   | neutral | thin neutral       | none   |
| Inverted    | `neutral`     | white   | none               | none   |

### Cards

- Light background (`#FFFFFF` or `background`)
- Rounded corners (`16px` – `24px`)
- Soft shadow
- Optional subtle border: `rgba(0, 0, 0, 0.05)`

### Inputs

- Light background
- Rounded edges (`12px` – `16px`)
- Subtle border
- **Focus state:** border color `primary`, optional soft shadow

### Icons & Actions

- Circular icon buttons
- Filled with accent colors
- White icons
- Consistent sizing

## Visual Style Rules

- No neon or glow effects
- Soft contrast (avoid pure black — use `neutral`)
- Use spacing + radius for hierarchy
- Clean and minimal layout

## Layout Patterns

- Grid-based layout
- Generous spacing
- Grouped cards
- Consistent alignment

## Bootstrap 5 Mapping

When implementing with Bootstrap 5, apply these design tokens via CSS custom properties:

```css
:root {
    --bs-primary: #FF2D78;
    --bs-secondary: #00FFCC;
    --bs-warning: #FFE04A;
    --bs-dark: #28283E;
    --bs-body-bg: #F4F5FB;
    --bs-body-color: #28283E;
    --bs-border-radius: 1rem;
    --bs-border-radius-lg: 1.5rem;
    --bs-box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
}
```
