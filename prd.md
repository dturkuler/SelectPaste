Below is a **Product Requirements Document (PRD)** for the tool you described.

---

# Product Requirements Document (PRD)

## Product Name (working)

**Command Palette Injector (CPI)**

---

## 1. Purpose

Create a **portable Windows executable** that provides a **global hotkey-driven command palette**. The palette displays a **readable list of preset items**. When the user selects one, the program **injects a different predefined command text** into the currently focused input field (for example, the Antigravity chat box).

The tool must work **outside of VS Code**, **outside of Antigravity**, and be completely **system-level**, so it functions in any application that accepts keyboard input.

---

## 2. Problem Statement

Existing solutions fail because:

* VS Code snippets, tasks, and extensions do not work inside Antigravity’s chat input.
* Text expanders rely on triggers, not a visible menu.
* Espanso forms are fragile and complex to configure.
* Clipboard managers do not provide labeled command menus.
* No lightweight portable tool exists that provides a **hotkey → menu → inject text** flow.

---

## 3. Goals

The application must:

1. Be a **single portable `.exe`**. No installation required.
2. Register a **global hotkey** (example: `Ctrl + Alt + .`).
3. Show a **popup window** with a vertical, scrollable list.
4. Each list item shows a **label** but inserts a **different value**.
5. Inject the selected value into **whatever input is currently focused**.
6. Work in:

   * Antigravity chat box
   * VS Code
   * Browsers
   * Terminals
   * Any text input field
7. Be easy to edit. Presets stored in a simple config file (JSON/YAML).
8. Start instantly and consume minimal resources.
9. Require zero dependencies after build.

---

## 4. Non-Goals

* Not a clipboard manager.
* Not a snippet expander.
* Not IDE dependent.
* No cloud sync.
* No plugin system.
* No complex UI.

---

## 5. User Workflow

### Normal Use

1. User is typing in Antigravity chat.
2. Presses `Ctrl + Alt + .`
3. A small window appears at center of screen.
4. Shows:

```
1. new-project
2. map-codebase
3. execute-phase
...
```

5. User navigates with arrow keys.
6. Presses Enter.
7. Window closes.
8. `gsd:new-project` is typed into the chat box as if typed manually.

---

## 6. Functional Requirements

### FR1. Global Hotkey

* Register system-wide hotkey.
* Customizable in config.

### FR2. Popup Command Palette

* Always on top.
* Centered.
* Keyboard navigable.
* Supports search/filter as user types.

### FR3. Label vs Injected Text

Each entry contains:

```json
{
  "label": "1. new-project",
  "value": "gsd:new-project"
}
```

### FR4. Text Injection Method

Must simulate **real keyboard typing**, not paste, not clipboard.

Use Windows API:

* `SendInput` for keystroke injection.

### FR5. Config File

External editable file:

`commands.json`

Example:

```json
[
  { "label": "1. new-project", "value": "gsd:new-project" },
  { "label": "2. map-codebase", "value": "gsd:map-codebase" }
]
```

App reloads config on launch.

### FR6. Search

Typing filters list in real time.

### FR7. Portable

No installer. No registry writes.

---

## 7. UX Requirements

* Dark minimal UI.
* No mouse required.
* Arrow keys + Enter.
* Esc closes.
* Fast open. Under 50 ms.

---

## 8. Technical Requirements

### Recommended Stack

This is ideal for:

* **AutoHotkey v2** compiled to exe
  or
* **Python + PyQt / Tkinter** compiled with PyInstaller
  or
* **C# .NET WinForms/WPF** single file publish

Best balance: **AutoHotkey v2**

Why:

* Native hotkeys
* Native GUI menus
* Native SendInput
* Single exe compile
* No runtime

---

## 9. Architecture

```
[Hotkey Listener]
        ↓
[Popup GUI]
        ↓
[User Selection]
        ↓
[SendInput(value)]
        ↓
[Return focus]
```

---

## 10. Performance Requirements

* Memory < 30 MB
* Startup < 200 ms
* Injection latency < 10 ms

---

## 11. Future Enhancements (optional)

* Categories / folders
* Multiple palettes
* Editable UI
* Import/export presets

---

## 12. Success Criteria

You press a hotkey in Antigravity chat.
You see a readable menu.
You select an item.
The correct hidden command appears in chat.

No tricks. No triggers. No extensions.

---

## 13. Deliverables

* `CPI.exe`
* `commands.json`
* README

---

If you want, next step is to turn this PRD into the **actual AutoHotkey implementation** in one file that you can compile to exe.
