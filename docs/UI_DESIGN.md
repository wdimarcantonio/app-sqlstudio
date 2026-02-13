# UI Design Documentation

## Overview
This document describes the UI design system implemented for the SqlStudio ETL application, including the new shell layout, component structure, and design tokens.

## Design System

### Color Palette

#### Primary Colors
- **Primary**: `#0066CC` - Main brand color for buttons and highlights
- **Primary Hover**: `#0052A3` - Hover state for primary elements
- **Primary Light**: `#E6F2FF` - Light background for primary elements

#### Status Colors
- **Success**: `#28A745` - Success states and positive actions
- **Error**: `#DC3545` - Error states and destructive actions
- **Warning**: `#FFC107` - Warning states
- **Info**: `#17A2B8` - Informational states

#### Task Type Colors (for future pipeline visualization)
- **Source**: `#6F42C1` - Data source tasks
- **Transform**: `#FD7E14` - Data transformation tasks
- **Load**: `#20C997` - Data loading tasks
- **API**: `#E83E8C` - API integration tasks

#### Neutral Colors
- **Background Dark**: `#1E1E1E` - Sidebar and dark UI elements
- **Background Medium**: `#2D2D30` - Secondary dark elements
- **Background Light**: `#F5F5F5` - Main content background
- **Text Primary**: `#333333` - Primary text color
- **Text Secondary**: `#666666` - Secondary text color
- **Border**: `#DDDDDD` - Border color

### Typography
- **Font Primary**: 'Segoe UI', system-ui, sans-serif
- **Font Mono**: 'Cascadia Code', 'Consolas', monospace

#### Font Sizes
- **Extra Small**: `0.75rem` (12px)
- **Small**: `0.875rem` (14px)
- **Base**: `1rem` (16px)
- **Large**: `1.125rem` (18px)
- **Extra Large**: `1.25rem` (20px)
- **2X Large**: `1.5rem` (24px)

### Spacing System
- **XS**: `4px`
- **SM**: `8px`
- **MD**: `16px`
- **LG**: `24px`
- **XL**: `32px`

## Layout Structure

### App Container
The main application uses a flex-based layout with a fixed sidebar and flexible content area.

```
┌─────────────────────────────────────────┐
│ Sidebar (240px / 60px collapsed)       │ TopBar (60px height)
│ ┌───────────────────────────────────────┤
│ │ Logo                                  │ User Info
│ ├───────────────────────────────────────┤
│ │                                       │
│ │ Navigation Links                      │ Content Area
│ │ - Home                                │
│ │ - Connections (with badge)            │
│ │ - Pipelines                           │
│ │ - Import Data                         │
│ │ - History                             │
│ │ - Settings                            │
│ │                                       │
│ ├───────────────────────────────────────┤
│ │ Collapse Button                       │
│ └───────────────────────────────────────┘
└─────────────────────────────────────────┘
```

### Sidebar Behavior
- **Default width**: 240px
- **Collapsed width**: 60px
- **Transition**: 0.3s ease
- Shows icons only when collapsed
- Navigation links highlight active page
- Collapsible via button in footer

### Responsive Design
- **Desktop**: Full sidebar (240px)
- **Tablet**: Collapsed sidebar (60px)
- **Mobile**: Hidden sidebar with hamburger menu (future enhancement)

## Components

### MainLayout
- Container for the entire application
- Manages sidebar collapse state
- Includes error boundaries
- Renders ToastContainer for notifications
- Displays loading overlay when needed

### MainSidebar
- Fixed position navigation
- Collapsible functionality
- Active link highlighting
- Connection count badge
- Navigation dividers for grouping

### TopBar
- Fixed height (60px)
- Application title/breadcrumbs
- User information/menu (placeholder)
- White background with subtle border

### Connection Cards
Visual representation of database and API connections with:
- Color-coded top border by connection type
- Icon representing connection type
- Connection name and type label
- Status indicator (Active/Inactive)
- Action buttons (Test, Edit, Delete)
- Last tested timestamp

#### Connection Type Colors
- **SQL Server**: `#CC2927` (Red)
- **PostgreSQL**: `#336791` (Blue)
- **MySQL**: `#00758F` (Teal)
- **WebService**: `#E83E8C` (Pink)
- **Excel**: `#217346` (Green)
- **CSV**: `#FFA500` (Orange)

### Empty States
Centered content with:
- Large emoji icon
- Heading
- Descriptive text
- Call-to-action button

## CSS Architecture

### File Structure
```
wwwroot/css/
├── variables.css    - Design tokens and CSS custom properties
├── layout.css       - Layout components and utilities
├── sidebar.css      - Sidebar-specific styles
├── connections.css  - Connection pages styles
└── app.css          - Legacy application styles
```

### Loading Order
1. `variables.css` - Must load first for CSS custom properties
2. `layout.css` - Core layout styles
3. `sidebar.css` - Sidebar component
4. `connections.css` - Feature-specific styles
5. `app.css` - Application-specific overrides

## Accessibility

### Color Contrast
All color combinations meet WCAG AA standards:
- Primary text on white background: 4.5:1
- Secondary text on white background: 4.5:1
- White text on primary: 4.5:1

### Keyboard Navigation
- All interactive elements are keyboard accessible
- Focus states visible with outline
- Tab order follows logical flow

### Screen Readers
- Semantic HTML elements used throughout
- ARIA labels for icon buttons
- Status messages announced appropriately

## Future Enhancements

### Phase 2
- Mobile responsive navigation with hamburger menu
- Theme switcher (light/dark mode)
- User profile dropdown in TopBar
- Breadcrumb navigation for nested pages

### Phase 3
- Advanced notification system with toast queue
- Keyboard shortcuts
- Customizable sidebar pin/unpin
- Drag-and-drop reordering in lists

## Browser Support
- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)

## Performance Considerations
- CSS custom properties for efficient theming
- Minimal animations (0.3s transitions only)
- Lazy loading for Monaco editor
- Image assets optimized and minimal

---

Last Updated: 2026-01-30
Version: 1.0.0
