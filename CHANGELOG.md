# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## [1.0.5] - 2026-02-08
### Added
- **Conditional Breadcrumbs**: Category names (e.g., `[GIT]`) now appear instantly during global search but remain hidden when browsing specific tabs for a cleaner look.
- **Value Visibility**: Search results now display a preview of the text snippet (`Label -> Value`) directly in the list.

## [1.0.4] - 2026-02-08
### Fixed
- **Version Reporting**: Corrected a bug where the "About" dialog and startup popup incorrectly showed "1.0.0" instead of the current release version.

## [1.0.3] - 2026-02-08
### Added
- **Frequency-Based Sorting**: The app now tracks how often you use specific commands. Your most frequent commands automatically "drift" to the top of results.
- **Usage Statistics**: Added usage count tracking to the ToolTip information.

## [1.0.1] - 2026-02-08
### Changed
- **Asset Restructuring**: Moved icons and example configurations to an `assets/` directory for better project organization.
- **Self-Contained Deployment**: Improved release packaging for single-file executables.

## [1.0.0] - 2024-02-08
### Added
- Initial release of SelectPaste.
- **Tabbed Interface**: Organized commands into groups.
- **Global Search**: Search across all command tabs instantly.
- **System Tray Icon**: "SP" icon with context menu for Config and Quit.
- **Configurable Hotkeys**: Global hotkey support via `settings.json`.
- **JSON Configuration**: `commands.json` for managing snippets.
- **Portable**: Compiled as a self-contained single-file executable.
