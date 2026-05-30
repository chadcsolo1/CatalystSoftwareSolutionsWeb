# Catalyst Software Solutions - Brand Color Scheme

## Color Palette

### Primary Colors
- **Catalyst Orange** — `#F26A2C`  
  Main action color, CTAs, primary buttons

- **Catalyst Blue** — `#1F6FB2`  
  Main brand color, icons, links

### Secondary Colors
- **Catalyst Red** — `#D6453D`  
  Alerts, warnings, challenge indicators

- **Deep Navy** — `#0F2A4A`  
  Headings, dark text, professional tone

### Accent Colors
- **Warm Amber** — `#F4A259`  
  Subtle highlights, hover states

- **Sky Blue** — `#5FAFE3`  
  Focus states, informational elements

- **Off‑White** — `#F7F9FC`  
  Backgrounds, light sections

## Brand Personality
**Dynamic, modern, energetic, and forward‑thinking**  
A brand that communicates innovation, momentum, and reliability.

## Usage Guidelines

### Gradients
- **Primary Gradient**: Orange to Blue (`#F26A2C` → `#1F6FB2`)
- **Accent Gradient**: Amber to Sky Blue (`#F4A259` → `#5FAFE3`)

### Text Colors
- **Primary Text**: Deep Navy (`#0F2A4A`)
- **Secondary Text**: Gray (`#718096`)
- **Light Text**: White (`#FFFFFF`)

### Backgrounds
- **Light Sections**: Off-White (`#F7F9FC`)
- **White Sections**: Pure White (`#FFFFFF`)
- **Hero Sections**: Primary Gradient

### Interactive Elements
- **Primary Buttons**: Catalyst Orange
- **Primary Button Hover**: Catalyst Red
- **Links**: Catalyst Blue
- **Focus States**: Sky Blue

## CSS Variables Reference

All colors are defined in `/wwwroot/app.css`:

```css
--catalyst-orange: #F26A2C
--catalyst-blue: #1F6FB2
--catalyst-red: #D6453D
--deep-navy: #0F2A4A
--warm-amber: #F4A259
--sky-blue: #5FAFE3
--off-white: #F7F9FC
```

## Implementation Notes

- Navbar uses primary gradient
- Hero sections use primary gradient
- Feature icons use Catalyst Blue
- Hover states transition to Catalyst Orange
- Stats and numbers use Catalyst Orange
- Section headings use Deep Navy
- Light backgrounds use Off-White
