and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.2] - 2026-02-16
### Added
- **Settings Editor UI**: Integrated a new graphical settings editor to manage hotkeys, window dimensions, font sizes, and theme colors without manual JSON editing.
- **Hotkey Recorder**: A dedicated field in settings that captures your key combinations automatically.
- **Visual Color Picker**: Integrated a system color picker and live preview swatches for palette customization.
- **TopMost Priority Handling**: Improved Z-order management to ensure the Settings Editor and Color Pickers always stay on top of the command palette.

### Fixed
- **Settings Visibility**: Fixed an issue where the settings window could be hidden behind the command palette in certain focus states.

## [1.2.1] - 2026-02-16
### Added
- **Command & Group Manager**: Integrated a dedicated UI for adding, editing, deleting, and moving commands and groups, accessible via a new system command `::MANAGE_COMMANDS::`.
- **Startup Visibility**: The application now launches with the Command Palette visible for immediate use after the initial welcome dialog.
- **Scrollable Manager UI**: Added vertical scrollbars and optimized the layout of the command list in the manager for better navigation.

### Fixed
- **Deep Z-Order Handling**: Ensured management dialogs (Move To, Group Edit) correctly appear as TopMost above the palette to prevent window hiding.
- **Form Layout Refinement**: Compacted the Value and Description fields in the editor to provide more space for the command list and groups.

## [1.2.0] - 2026-02-16
### Added
- **Multiple Profiles**: Added support for switching between different command configuration files (e.g., `gsd.json`, `design.json`).
- **Profile Switcher**: New built-in "Switch Profile" command to easily change the active command set.
- **Per-Profile Usage Stats**: Usage statistics are now tracked separately for each profile (e.g., `gsd_usage.json`).

## [1.1.2] - 2026-02-16
### Fixed
- **Sorting Logic**: Ensure "Most Used" sorting applies only to the "Favorites" tab. All other tabs are now sorted alphabetically by label for easier scanning.

## [1.1.1] - 2026-02-10
### Fixed
- **Duplicate Search Results**: Fixed a bug where global search would show duplicate entries by inadvertently searching through the generated "Favorites" group. Search now correctly filters unique commands from the original categories.

## [1.1.0] - 2026-02-10
### Added
- **Dynamic Favorites Group**: A virtual "Favorites" category is now automatically created at startup. It displays your top 15 most-used commands across all groups, prioritized by usage frequency.
- **Auto-Scrolling Tab Bar**: Category tabs now automatically scroll into view when navigating with arrow keys.
- **Viewport System**: Completely redesigned the header layout using a viewport system to enable smooth tab scrolling without bulky native scrollbars.
- **Invisible Scrollbars**: Implemented Win32 API calls to suppress horizontal and vertical scrollbars in the header for a cleaner aesthetic.

### Fixed
- **Header Layout Precision**: Fixed an issue where group names were being cut off or hidden due to layout calculation errors during scrolling.
- **UI Scaling**: Improved dynamic scaling for the tab header to properly accommodate larger font sizes and long category names.

## [1.0.8] - 2026-02-08
### Added
- **Single Instance Enforcement**: The app now automatically detects and terminates any existing background instances of SelectPaste on startup. This ensures a clean reload and prevents hotkey or tray icon conflicts.

## [1.0.7] - 2026-02-08
### Added
- **UI Customization**: Added `FontSize`, `LabelColor`, `ValueColor`, and `CategoryColor` to `settings.json`.
- **Owner-Drawn List**: Enhanced text rendering with multi-color support for labels, values, and categories.
- **Dynamic Layout**: UI controls now automatically scale and fill the window space correctly, removing empty margins.
- **Settings Integrity**: Implemented a more robust settings serializer that preserves custom metadata (like `_help` comments) and avoids JSON character escaping for better human readability.

### Fixed
- **UI Alignment**: Minimized gap between tab header and close button for a cleaner header design.
- **Search Precision**: Refined search logic to strictly match against labels and values only, improving result accuracy.

## [1.0.6] - 2026-02-08
### Added
- A minimal, keyboard-centric command palette for Windows that lets you paste pre-defined text snippets instantly.
- **Resizable Window**: The command palette is now resizable. Drag any edge or corner to adjust the size.
- **Persistent Window State**: The app now remembers your custom window size and position across sessions.
- **Draggable Header**: The palette can be moved by dragging the top tab area.
- **Search Optimization**: Search now specifically targets labels and values, ignoring descriptions for cleaner results.
```
