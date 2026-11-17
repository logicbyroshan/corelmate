# Changelog

See [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 3 packaging details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 4 Badge Generator details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 5 workflow hardening details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 6 Convert Text to Curves details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 7 CSV/XLSX import details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Project branding and open-source documentation details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).
 
Task 9 Modern UI Design System details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

## CorelMate 1.18.1 (Update 2) - Docker UI Display & PowerClip Curve Conversion Fix

- **Docker UI Rendering Fix**: Resolved root cause in `CorelMatePanel.xaml` where `Style="{StaticResource CorelMateRootStyle}"` on `<UserControl>` threw a `XamlParseException: Cannot find resource named 'CorelMateRootStyle'`, leaving the Docker interior completely blank. Direct root styling now ensures 100% visible, themed WPF controls upon Docker creation.
- **PowerClip & Nested Group Curve Conversion**: Hardened `CorelShapeTraversal` and `CorelTextToCurvesConverter` to safely traverse and convert text objects inside nested groups and PowerClips without ungrouping, with automatic fallback to container edit mode (`PowerClip.EnterEditMode()` / `LeaveEditMode()`).
- **Dashboard Quick Action**: Added instant `Convert Page ⚡` 1-click action directly to the CorelMate Dashboard tab for immediate single-click curve conversion without manual tab navigation.
- **Navbar Menu Action Routing**: Updated menu command bindings in `CorelWorkspacePatcher` so clicking `Convert Page to Curves (Docker)` or other CorelMate tools in the top navigation bar immediately activates and displays the CorelMate Docker.
- **Shape Traversal Resilience**: Added exception guards around shape inspection properties (`IsHidden`, `IsLocked`, `Type`) and combined Layer traversal with `page.Shapes` enumeration to guarantee zero skipped text elements across complex prepress files.

## CorelMate 1.18.1 (Update 1) - Docker Title, Navbar Integration & Accessibility Fix

- **Docker Title Correction**: Fixed Docker title and vertical tab caption displaying `CorelMate.UI.CorelMatePanel`; now cleanly and permanently displays `CorelMate` across all windows and dockers trays.
- **Native Navbar Integration**: Corrected top navigation bar integration in CorelDRAW (`_default.cdws` `workspace.xml`) across Welcome Screen and Document Open modes, placing `CorelMate` directly on the main menu bar alongside `File | Edit | ... | Window | Help | CorelMate`.
- **Closed Docker Accessibility**: Added permanent entry points to re-access and toggle the Docker when closed via:
  - Main navigation menu: `CorelMate -> CorelMate Docker` (native `*Docker(...)` binding with live checkmark status)
  - Window menu: `Window -> CorelMate Docker`
  - Right-side vertical dockers tray tab strip (`CorelMate` tab)
  - Quick launch script: `scripts/Show-CorelMate.ps1`
- **CorelWorkspacePatcher**: Built host-independent, pure XML and ZIP in-place workspace patcher in `CorelMate.Core` covered by executable smoke tests in `CorelMate.Tests`.

## CorelMate 1.18.1 - Production Release

- **Official Release Designation**: Promoted CorelMate to official release `v1.18.1`.
- **Reference Extension Reverse Engineering & Audit**: Decompiled and analyzed `CdrPreflight Installer X7-2020` (NSIS installer, MS-OVBA VBA decompilation across 11 modules).
- **Page-to-Curves & Multi-Scope Conversion**: Native, non-destructive vector curve conversion supporting `CurrentPage`, `AllPages`, and `SelectedObjects`.
- **Document Safety & Preservation**: Automated pre-conversion backup copy preservation (`_curves.cdr`) via `SaveAsCopy`/`File.Copy` and atomic undo protection (`BeginCommandGroup`/`EndCommandGroup`).
- **CorelDRAW Navigation Menu Integration**: CorelMate appears as the last main navigation menu item in the CorelDRAW Menu Bar (`f3016f3c-2847-4557-b61a-a2d05319cf18`) with dynamic commands and companion `CorelMate.gms` macro.
- **Version Adaptation**: Full support across CorelDRAW 2017 through 2026 (v19 to v28) with 5 authentic era design themes.
- **Batch Badge Generator**: Automated multi-page layout planning with CSV and XLSX spreadsheet data import.
- **Document & Object Inspector**: Real-time shape enumeration, color model inspection, and unique font discovery.
- **Hardened Packaging**: Comprehensive distribution packaging with non-elevated installation support (`Install-CorelMate.ps1`).

## CorelMate 0.2.0 - UI Redesign, Version Adaptation & Native Menu Integration

- **Central Version Compatibility Registry**: Added support and explicit validation for CorelDRAW versions 2017 through 2026 (v19 to v28), with legacy (< v19) rejection and safe detection.
- **Version-Adaptive UI System**: Added authentic version era design profiles:
  - `Theme2017.xaml`: Classic Light flat design.
  - `Theme2019.xaml`: Charcoal dark/medium gray design.
  - `Theme2021.xaml`: Graphite & Teal modern design.
  - `Theme2023.xaml`: Slate & Electric Azure design.
  - `Theme2026.xaml`: Adaptive Pro Zinc & Emerald modern design.
  - Dynamic `CorelMateThemeManager` with runtime era resolution and manual override testing.
- **Native CorelDRAW Navigation Integration**:
  - Registered CorelMate as the LAST main navigation menu item in the CorelDRAW Menu Bar (`f3016f3c-2847-4557-b61a-a2d05319cf18`).
  - Native Addon XSLT stylesheet (`UserDraw.xslt`) and zero-byte `Coreldrw.addon` deployed to User AppData and system Addon directories for restart persistence.
  - Companion `CorelMate.gms` macro deployed for seamless menu-to-action routing.
- **Document & Object Inspector**:
  - Real-time document analysis: page count, page dimensions (mm), color model, shape counts breakdown (text, curves, bitmaps, rectangles, ellipses, groups).
  - Font analyzer: scans active artwork for all unique fonts and counts.
- **Page-to-Curves & Multi-Scope Text Conversion**:
  - Implemented full multi-scope text-to-curves conversion: `CurrentPage`, `AllPages`, and `SelectedObjects`.
  - Non-destructive recursive traversal: preserves nested group structures and inspects PowerClip contents without ungrouping.
  - Document safety mechanism: option to automatically save an uncorrupted backup copy (`_curves.cdr`) via `SaveAsCopy`/`File.Copy` prior to conversion.
  - Performance optimization: wraps execution in atomic `BeginCommandGroup` with host optimization flags (`Optimization = True`, `EventsEnabled = False`, `PreserveSelection = False`) and screen redraw on finish.
  - Preflight analysis metrics: detailed breakdown of found text (artistic vs paragraph vs text on path), groups, PowerClips, convertible vs locked/hidden shapes.
  - Native menu commands in `UserDraw.xslt` and companion `CorelMate.gms` macro (`ConvertPageToCurves`, `ConvertAllPagesToCurves`, `ConvertSelectionToCurves`).
  - Redesigned Curves UI tab in `CorelMatePanel.xaml` with scope radio selectors, backup preservation toggle, live metric tiles, and execution duration reporting.
- **Reference Extension Reverse Engineering**:
  - Decompiled and audited `CdrPreflight Installer X7-2020` (extracted NSIS installer, decompressed MS-OVBA streams for 11 modules).
  - Evaluated architectural patterns and eliminated reference flaws (deprecated MSHTML docker, lack of backup copies, destructive layer unlocking, and silent error silencing).
- **Installer & Packaging**:
  - Multi-version detection in `Install-CorelMate.ps1` and `Install-Dev.ps1`.
  - Non-elevated User AppData deployment for unprivileged environments.
  - Full ZIP packaging with automated safety validation.