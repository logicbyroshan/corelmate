# Changelog

See [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 3 packaging details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 4 Badge Generator details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 5 workflow hardening details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 6 Convert Text to Curves details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Task 7 CSV/XLSX import details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

Project branding and open-source documentation details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).
 
Task 9 Modern UI Design System details are recorded in [.agents/CHANGELOG.md](.agents/CHANGELOG.md).

## CorelMate 1.18.1 (Update 5) - Single Master CorelMate Menu Before Help with Distinct Icons

- **Single Master Menu on Navbar**: Grouped all tools (`Curves`, `Export`, `Color`) under a single top-level `CorelMate` menu on the main navigation bar, keeping the CorelDRAW navbar clean and organized.
- **Menu Placement Before Help**: Positioned `CorelMate` immediately before the `Help` menu item (`... | Tools | Window | CorelMate | Help`) in both Welcome Screen (`6d1d9e94-13f3-4517-a976-ba9154671db4`) and Document Open (`76d73481-9076-44c9-821c-52de9408cce2`) menubar modes.
- **Distinct Authentic Feature Icons**: Attached native 16x16 `WCmnUI_UIItemBmp` base64 bitmaps (`userSmallBitmap`) across all menus and sub-commands:
  - `CorelMate` (Brand vector icon)
  - `Curves` flyout, `Curve Current Page`, `Curve All Pages`, and `Curve Selected Objects` (Vector curve node & pen icons)
  - `Export` flyout, `Export As ID Card`, and `Export As Print` (Document, badge card, and printer export icons)
  - `Color` flyout, `Convert to CMYK`, and `Convert to RGB` (Color palette, CMYK ink separation, and RGB channel icons)
- **Zero Docker Mode Preserved**: Operates without opening or displaying any Docker panel; every command executes directly with explicit native confirmation dialogs.
- **Installer Script Encoding Hardened**: Standardized all PowerShell installation scripts (`Install-Dev.ps1`, `Install-CorelMate.ps1`) to ASCII-compatible output formatting, eliminating CP1252 multibyte quote parser errors.

## CorelMate 1.18.1 (Update 4) - Direct Navbar Tools (Curves, Export, Color) with Zero Docker

- **Zero Docker Mode**: Removed Docker panel requirements completely. No Docker window is opened, docked, or shown. Operations execute directly within native CorelDRAW UI with standard native message dialogs.
- **Direct Top Navbar Menus**: Registered three distinct flyout menus directly in the top menu bar (Welcome Screen & Document Open modes):
  - **CorelMate Curves**:
    - `Curve Current Page`: Recursively converts all text shapes (including groups and PowerClips) on current page to curves.
    - `Curve All Pages`: Recursively converts all text across all pages in the active document to curves.
    - `Curve Selected Objects`: Converts only selected text shapes to curves.
  - **CorelMate Export**:
    - `Export As ID Card`: Directly exports text field metadata (name, text, font, size, coordinates, dimensions, color) to a `.json` file and publishes an editable vector `.pdf` template.
    - `Export As Print`: Publishes a print-ready CMYK PDF with text converted to curves and lossless bitmap quality.
  - **CorelMate Color**:
    - `Convert to CMYK`: Recursively converts all vector fills (uniform & fountain), outlines, and bitmap images (`cdrCMYKColorImage`) to CMYK.
    - `Convert to RGB`: Recursively converts all vector fills, outlines, and bitmap images (`cdrRGBColorImage`) to RGB.
- **Mandatory User Confirmation**: Every action prompts the user with an explicit confirmation dialog before executing.
- **Native MS-OVBA Binary GMS Companion**: Implemented a standalone MS-OVBA LZ77 compressor and OLE FAT sector allocator in `scripts/Build-Gms.py`, generating fully compliant binary `.gms` macros deployed to CorelDRAW's `Draw\GMS\` directory.
- **Unified Workspace & Addon Patcher**: Streamlined `CorelWorkspacePatcher.cs`, `scripts/Install-Dev.ps1`, and `packaging/Install-CorelMate.ps1` to automatically configure `_default.cdws` and deploy `UserDraw.xslt` without manual intervention.

## CorelMate 1.18.1 (Update 3) - Adaptive Light/Dark Theme, Live Connection & Streamlined UI

- **Connection Reliability (Fixed "Offline" Issue)**: Resolved root cause where `Marshal.GetActiveObject` threw `MK_E_UNAVAILABLE` because CorelDRAW does not register in the Windows Running Object Table (ROT). Implemented single-instance COM activation via `Type.GetTypeFromProgID("CorelDRAW.Application")` and `Activator.CreateInstance()`, backed by deferred retry timers and on-demand reconnection. Docker now connects immediately, showing `Connected` and live document information.
- **Adaptive Light & Dark Themes (Matches CorelDRAW Workspace)**: Added dedicated `LightTheme.xaml` and `DarkTheme.xaml` resource profiles with high-contrast typography and native surface styling. CorelMate automatically detects whether CorelDRAW and Windows are using light or dark mode via `AppsUseLightTheme`, and provides a 1-click ☀️ / 🌙 theme switcher in the Docker header.
- **Streamlined, Decluttered UI**: Consolidated 6 cramped tabs into 3 spacious, responsive tabs (`🔤 Curves`, `🏷️ Badges`, `⚙️ Tools & Info`). Cleaned up visual hierarchy, removed nested clutter cards, and made 1-click "Convert Text to Curves" the immediate primary workflow.
- **Hardened Curve Conversion & Auto-Unlock**: Added automatic unlocking of locked shapes (`UnlockLockedShapes`) and multi-stage fallback (PowerClip edit mode and active selection conversion) to guarantee complete curve conversion across nested prepress containers.


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