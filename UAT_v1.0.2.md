# User Acceptance Test (UAT) - v1.0.2 Features

**Objective**: Verify the new "Breadcrumbs" display and "Frequency Sorting" behavior in SelectPaste.

## 1. Breadcrumbs Test
**Step**:
1. Run `SelectPaste.exe`.
2. Press `Shift + Alt + .` (or your configured hotkey).
3. Type `git` in the search bar.

**Expected Result**:
- The list should show items like:
    - `[GIT] git status`
    - `[GIT] git commit`
- The `[GIT]` prefix (the "breadcrumb") tells you exactly which group the command belongs to.

## 2. Usage Tracking Test (Frequency Sorting)
**Step**:
1. Open the palette (`Shift + Alt + .`).
2. Search for `git commit`.
3. Select it and press **Enter** (this executes it and increments its count).
4. Repeat this 3 times.
5. Close and reopen the palette.
6. Type `git`.

**Expected Result**:
- `[GIT] git commit` should appear **higher** in the list than `[GIT] git push` or `[GIT] git status`, because you used it more often.
- Hover over `git commit`. The tooltip should say something like:
  > **Used: 3 times**

## 3. Version Check
**Step**:
1. Right-click the SelectPaste tray icon (near the clock).
2. Click **About**.

**Expected Result**:
- The dialog should say **SelectPaste v1.0.2** (once we perform the release).
- It should NOT say "1.0.0.0".

## 4. Release Folder Verification
**Step**:
1. Check the timestamp of `c:\projects\selectpaste\release\selectpaste\SelectPaste.exe`.

**Expected Result**:
- The "Date Modified" should be **today, right now** (indicating the latest build was preserved).
