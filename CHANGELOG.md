# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- **Resizable Window**: The command palette is now resizable. Drag any edge or corner to adjust the size.
- **Persistent Window State**: The app now remembers your custom window size and position across sessions.
- **Draggable Header**: The palette can be moved by dragging the top tab area.
- **Search Optimization**: Search now specifically targets labels and values, ignoring descriptions for cleaner results.
