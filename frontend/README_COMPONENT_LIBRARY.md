# Component Library

## Overview

The Component Library is an interactive showcase page for all custom components and common UI elements used in MTG Mullagain. Think of it as a built-in Storybook that helps with development, testing, and design consistency.

## Access

- **Development**: http://localhost:5173/components
- **Production**: /components

## Features

### 🔮 Mana Symbol Component
Interactive preview with:
- Text input field to test any mana pattern
- Size selector (small/medium/large)
- 7 pre-built examples covering all symbol types:
  - Colorless ({2}{3}{4})
  - Monocolored ({W}{U}{B}{R}{G})
  - Hybrid ({W/U}{U/B}{B/R}{R/G}{G/W})
  - Phyrexian ({W/P}{U/P}{B/P}{R/P}{G/P})
  - Mixed combinations
  - Special symbols ({T}{Q}{E}{C}{S}{X})
  - Complex examples with text

### 🃏 MTG Card Component
Displays both instant and creature cards with:
- Size controls
- Image-loading mode
- Text-only fallback mode
- Side-by-side comparisons

### 🔍 Scryfall Card Search
Live search component that:
- Can be toggled on/off
- Shows selected card details
- Demonstrates Scryfall syntax
- Displays search results with mana symbols

### 🔘 Buttons
Complete button showcase:
- Primary and secondary styles
- Colored variants (success, danger, warning, info)
- Disabled states
- Icon buttons with emojis
- Size variations

### 📊 Tables
Working table example featuring:
- MTG card data with mana symbols rendered inline
- Hover states
- Action buttons (edit, delete)
- Proper table structure and styling

### 📝 Forms
All form elements:
- Text inputs
- Textareas
- Select dropdowns
- Number inputs
- Checkboxes
- Radio buttons
- Complete form example with validation

### 📦 Cards & Containers
Various card styles:
- Standard cards
- Colored cards
- Success state cards
- Error state cards

## Layout

The component library uses a custom layout (`+layout.svelte`) that:
- Removes the top navigation bar for distraction-free viewing
- Maintains the app's styling and CSS imports
- Provides a clean, minimal interface

## Navigation

- **In-page navigation**: Scroll links to jump between sections
- **Back to app**: Button in the top-right corner
- **Smooth scrolling**: Anchored sections for easy reference

## Development

### Adding New Components

To add a new component to the library:

1. Create a new section in `/routes/components/+page.svelte`:

```svelte
<section id="your-component" class="bg-white rounded-lg shadow-md p-6">
  <h2 class="text-2xl font-bold text-gray-900 mb-4">Your Component</h2>
  <p class="text-gray-600 mb-6">Description...</p>
  
  <!-- Controls -->
  <!-- Preview -->
  <!-- Examples -->
</section>
```

2. Add navigation link:

```svelte
<a href="#your-component" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">
  Your Component
</a>
```

3. Include interactive controls for component props

### Best Practices

1. **Interactive Controls**: Add inputs/selects to toggle component states
2. **Multiple Examples**: Show various use cases and configurations
3. **Clear Labels**: Describe what each example demonstrates
4. **Visual Grouping**: Use colored backgrounds to separate examples
5. **Code Snippets**: Include example code where helpful

## Use Cases

### For Developers
- Quick reference for component APIs
- Visual testing during development
- Debugging component rendering
- Exploring component variations

### For Designers
- See all UI elements in one place
- Check consistency across components
- Test color schemes and spacing
- Verify accessibility features

### For Documentation
- Living style guide
- Component usage examples
- Visual proof that tests pass
- Design system reference

## Technical Details

### Files
- `/routes/components/+layout.svelte` - Custom layout without navigation
- `/routes/components/+page.svelte` - Main component library page

### Dependencies
All existing app dependencies are used. No additional packages required.

### Styling
Uses Tailwind CSS classes consistent with the rest of the application.

### State Management
Local component state for interactive controls. No global state needed.

## Testing

The component library page itself doesn't have dedicated tests, but it:
- Uses components that have 58 passing tests
- Serves as a visual integration test
- Helps verify component behavior in different states
- Aids in manual QA testing

## Future Enhancements

Potential additions:
- Dark mode toggle
- Copy code snippets button
- Component prop documentation
- Performance metrics
- Accessibility checker
- Export design tokens

---

**Note**: This is a development/documentation tool. Consider adding authentication if deploying to production to keep it internal-only.
