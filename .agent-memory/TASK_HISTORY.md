# Task History

## 2026-11-17: Fix Docker Connection ("Offline" State), Light/Dark Auto-Theme & Declutter UI

Task: Fix 3 reported issues in live CorelDRAW 2026 UI:
1. Docker status showed "Offline" / "CorelDRAW is not connected" because connection failed on load.
2. UI was dark while CorelDRAW was in light theme — theme must match CorelDRAW automatically.
3. UI was cluttered with 6 squeezed tabs and nested cards — streamline to clean, spacious, easy-to-use interface.
4. Hardened page-to-curves conversion with shape auto-unlocking and secondary PowerClip fallbacks.

Reason: User testing showed "Offline" warning, dark color mismatch with light CorelDRAW theme, and cluttered interface.

Files/areas affected: `src/CorelMate.Host/ICorelDrawHost.cs`, `src/CorelMate.Host/CorelTextToCurvesConverter.cs`, `src/CorelMate.Host/CorelShapeTraversal.cs`, `src/CorelMate.UI/CorelMatePanel.cs`, `src/CorelMate.UI/CorelMatePanel.xaml`, `src/CorelMate.UI/Themes/CorelMateThemeManager.cs`, `src/CorelMate.UI/Themes/LightTheme.xaml`, `src/CorelMate.UI/Themes/DarkTheme.xaml`, `tests/CorelMate.Tests/Program.cs`, `.agent-memory/`, `CHANGELOG.md`.

What changed:
1. Fixed Connection Root Cause: `Marshal.GetActiveObject("CorelDRAW.Application")` failed with `MK_E_UNAVAILABLE` (0x800401E3) because CorelDRAW does not register in the Running Object Table (ROT). Replaced with single-instance COM activation via `Type.GetTypeFromProgID("CorelDRAW.Application")` and `Activator.CreateInstance()`, which connects instantly to the running CorelDRAW process without launching another instance.
2. Connection Resilience & Auto-Reconnect: Added a deferred retry timer (up to 10 attempts) for graceful Docker initialization, plus on-demand reconnection on every user action button click.
3. Adaptive Light/Dark Theme: Added `LightTheme.xaml` and `DarkTheme.xaml` palettes. Implemented `DetectSystemTheme()` in `CorelMateThemeManager` reading `AppsUseLightTheme` from Windows registry. Added a 1-click ☀️ / 🌙 theme toggle in the Docker header.
4. Streamlined 3-Tab UI Redesign: Consolidated 6 cramped tabs into 3 spacious tabs (`🔤 Curves`, `🏷️ Badges`, `⚙️ Tools & Info`), bringing the 1-click "Convert Text to Curves" to the immediate foreground.
5. Hardened Curve Conversion: Added `UnlockLockedShapes` option to `CorelTextToCurvesConverter` and `CorelShapeTraversal` to automatically unlock locked text, plus secondary selection conversion fallback for deeply nested PowerClip elements.

Testing performed:
- Live COM connection test with running CorelDRAW v27.2.0.135 passed immediately.
- System theme detection returned `Light` as expected.
- All unit and smoke tests passed in `CorelMate.Tests`.
- Dev installation script `Install-Dev.ps1` completed cleanly.
- Release build passed with 0 warnings, 0 errors.

Follow-up: Merge via PR to `main`.


## 2026-11-17: Fix Docker UI Rendering & PowerClip/Group Curve Conversion Workflow

Task: Fix 2 reported issues:
1. Docker inside showing no UI (blank gray screen inside Docker panel).
2. Clicking Convert Page to Curves did not convert text on page / inside PowerClips / groups.

Reason: Address user testing feedback with real prepress artwork (`academic heights public school.cdr`).

Files/areas affected: `src/CorelMate.UI/CorelMatePanel.xaml`, `src/CorelMate.UI/CorelMatePanel.cs`, `src/CorelMate.Host/CorelShapeTraversal.cs`, `src/CorelMate.Host/CorelTextToCurvesConverter.cs`, `src/CorelMate.Core/CorelWorkspacePatcher.cs`, `packaging/Install-CorelMate.ps1`, `scripts/Install-Dev.ps1`, `tests/CorelMate.Tests/Program.cs`, `.agent-memory/`, `CHANGELOG.md`.

What changed:
1. Identified root cause for blank Docker: `CorelMatePanel.xaml` had `Style="{StaticResource CorelMateRootStyle}"` on root `<UserControl>`, which threw `XamlParseException: Cannot find resource named 'CorelMateRootStyle'` because the resource dictionary is defined within the UserControl itself. Replaced with direct property setters on UserControl. Instantiation now verified 100% clean.
2. Identified root cause for menu click doing nothing: In `_default.cdws`, `ItemConvertPageCurvesGuid` was declared as an unattached `type="button"` with no command binding. Changed to `check="*Docker('{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}')"` and bound native macro `ConvertAllToCurves.Converter.ConvertAllToCurves` to `ItemConvertAllCurvesGuid`.
3. Added 1-click `Convert Page ⚡` button directly on the CorelMate Dashboard tab for instant active page conversion without having to navigate tabs.
4. Hardened `CorelShapeTraversal.FlattenPage` to traverse both Layers and `page.Shapes` with a visited static ID set to ensure no text shapes are missed.
5. Hardened `CorelTextToCurvesConverter.Convert` to convert text inside PowerClip containers with automatic fallback to `PowerClip.EnterEditMode()` / `LeaveEditMode()`.
6. Verified conversion against real user artwork (`academic heights public school.cdr`): Converted all 2 text objects on Page 1 in 123ms with 0 errors and 0 remaining text shapes.

Testing performed:
- Standalone WPF instantiation test passed (Grid content, background #FF18181B, all tabs and buttons found).
- All unit and smoke tests passed in `CorelMate.Tests`.
- Real document conversion test on `academic heights public school.cdr` passed cleanly.
- Release build passed with 0 warnings, 0 errors.

Follow-up: Merge via PR to `main`.

## 2026-11-17: Fix Docker Title Caption, Main Navbar Integration & Closed Docker Accessibility

Task: Fix 3 reported issues in live CorelDRAW 2026 UI:
1. Docker title and tab strip displaying full C# class name `CorelMate.UI.CorelMatePanel` instead of `CorelMate`.
2. CorelMate missing from top navigation bar (navbar: `File | Edit | ... | Window | Help | CorelMate`).
3. Re-accessing Docker when closed: provide seamless UI entry points (navbar, Window menu, dockers tab tray, and helper script).

Reason: Address user testing feedback and screenshots showing Docker header and missing navbar menu.

Files/areas affected: `src/CorelMate.Core/CorelWorkspacePatcher.cs`, `src/CorelMate.UI/CorelMatePanel.cs`, `src/CorelMate.Host/CorelMenuIntegration.cs`, `src/CorelMate.Host/CorelMate.Host.csproj`, `scripts/Install-Dev.ps1`, `scripts/Show-CorelMate.ps1`, `packaging/Install-CorelMate.ps1`, `tests/CorelMate.Tests/Program.cs`, `.agent-memory/`, `CHANGELOG.md`.

What changed:
1. Identified root cause in CorelDRAW active workspace `_default.cdws` (`content/workspace.xml`) where `AddDocker` generated `userCaption="CorelMate.UI.CorelMatePanel"`.
2. Created `CorelWorkspacePatcher` in `CorelMate.Core` to safely patch active workspaces in-place:
   - Sets `userCaption="CorelMate"` and `caption="CorelMate"` on `<dockerData guid="{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}" ...>`.
   - Injects `CorelMate` top-level flyout menu item into menubar `modeData` (`6d1d9e94...` Welcome & `76d73481...` Doc Open).
   - Injects `CorelMate Docker` toggle checkButton into Window menu and CorelMate menu using native `*Docker('{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}')`.
   - Configures right-side dockHost viewHost `bf2668a0-d25d-4eb8-97c6-c0f9fe6e7516` with the CorelMate tab.
   - Populates full submenu items (Curves, Inspector, Badges, About).
3. Exposed `Title => "CorelMate"` and `Caption => "CorelMate"` in `CorelMatePanel.cs`.
4. Added `scripts/Show-CorelMate.ps1` for rapid one-command Docker launch.
5. Added unit tests for workspace XML and in-place zip archive patching in `CorelMate.Tests`.
6. Updated `Install-Dev.ps1` and `packaging/Install-CorelMate.ps1` with safe timeout background COM execution.
7. Verified live installation in CorelDRAW 2026 (v27.2.0.135).

Testing performed:
- All unit and smoke tests passed in `CorelMate.Tests` (including XML and zip archive tests).
- Release build passed with 0 warnings and 0 errors.
- Package build via `Build-Release.ps1` succeeded and validated.
- Real-host installation via `Install-Dev.ps1` succeeded on CorelDRAW 2026 v27.2.0.135.

Follow-up: Merge through GitHub PR workflow to `main`.

## 2026-11-17: Master Engineering Audit, UI Redesign, Version Adaptation & Menu Integration

Task: Complete engineering audit, redesign the UI, integrate into the CorelDRAW application navigation menu, implement version-aware UI adaptation (2017–2026), and verify all features.

Reason: Fulfill Master Engineering Prompt requirements to transition CorelMate into a stable, version-adaptive, natively integrated CorelDRAW productivity companion.

Files/areas affected: `CorelMate.Core`, `CorelMate.Host`, `CorelMate.UI`, `CorelMate.Infrastructure`, `tests/CorelMate.Tests`, `scripts/`, `packaging/`, `.agent-memory/`, `CHANGELOG.md`.

What changed:
1. Created `CorelVersion`, `CorelInstallationInfo`, and `CorelCompatibilityRegistry` supporting CorelDRAW 2017 through 2026 (v19–v28).
2. Built `CorelVersionDetector` for multi-version registry and COM inspection.
3. Designed and added 5 authentic version design profiles (`Theme2017.xaml`, `Theme2019.xaml`, `Theme2021.xaml`, `Theme2023.xaml`, `Theme2026.xaml`) managed dynamically by `CorelMateThemeManager`.
4. Implemented native CorelDRAW main navigation menu integration via Addon XSLT (`UserDraw.xslt` + `Coreldrw.addon`) and companion GMS macro (`CorelMate.gms`).
5. Implemented `CorelDocumentInspector` measuring active document metrics, shapes breakdown, and unique font analysis.
6. Redesigned `CorelMatePanel.xaml` into a compact, professional dashboard with 6 segmented views (Dashboard, Badges, Curves, Inspect, Compat, Settings).
7. Expanded `CorelMate.Tests` covering version compatibility, major version parsing, document metrics, file logging, badge engine math, placeholder replacement, and CSV/XLSX imports.
8. Enhanced `Install-CorelMate.ps1` and `Install-Dev.ps1` with multi-version detection, non-elevated user AppData deployment, and clean uninstallation.
9. Validated complete release packaging with `Build-Release.ps1` producing `artifacts/CorelMate-0.2.0-docker.zip`.

Important decisions: Modular boundary preserved; CorelDRAW COM types strictly isolated in `CorelMate.Host`; UI communicates via abstracted host properties; User AppData used for unprivileged installation.

Testing performed:
- All unit and smoke tests passed in `CorelMate.Tests`.
- Release build passed with 0 warnings and 0 errors.
- Real-host installation and Docker display in CorelDRAW 27.2.0.135 passed.
- Package build and packaging safety validation passed.

Follow-up: Merge through GitHub PR workflow to `main`.

Task: Audit the existing CorelMate repository and establish persistent agent memory.

Reason: Create a reliable operating system for future coding sessions without changing application code.

Files/areas affected: New `AGENTS.md` and `.agent-memory/` only. Existing application files were read, not modified.

What changed: Documented the actual modular .NET/CorelDRAW architecture, WPF Docker path, commands, conventions, risks, security boundaries, and mandatory GitHub PR workflow.

Important decisions: Preserve the existing host boundary; never change `main` directly; every future change must be pushed on a branch and merged through a PR.

Testing performed: Read-only audit. Existing recorded validation remains Release build passed, smoke test passed, and real CorelDRAW Docker lifecycle passed.

Follow-up: Publish the complete baseline from a non-main branch through `https://github.com/logicbyroshan/CorelMate.git`; then verify the merged repository state.

## 2026-09-16: Bootstrap Publication

Task: Publish the audited baseline and bootstrap memory through the required GitHub workflow.

Reason: Establish the first reviewable repository history without direct application changes on `main`.

Files/areas affected: Git metadata and remote publication; no application source changes.

What changed: Created `origin`, published `chore/bootstrap-agent-memory`, created PR #1, passed the GitGuardian check, squash-merged it into `main`, and deleted the feature branch.

Important decisions: Created an empty metadata-only main base because the new GitHub repository had no base branch; all application files entered `main` through PR #1.

Testing performed: Verified clean local `main`, `origin/main` alignment, and merged PR state.

Follow-up: For every future change, use a non-main branch, push it, create a PR with `gh`, pass checks/review, merge the PR, and clean up the branch.

## 2026-09-16: Task 3 Packaging

Task: Determine the supported production packaging/startup path.

Reason: Separate reproducible release packaging from the verified developer COM bootstrap.

Files/areas affected: `scripts/Build-Release.ps1`, `packaging/`, `.gitignore`, README, and project documentation.

What changed: Added a clean/test/validate ZIP release process and package-local session install/remove scripts. The package contains only the WPF Docker assembly and required session tooling.

Important decisions: Do not invent a `.addon` schema or automatic .NET startup mechanism. Core-owned interop assemblies are not redistributed.

Testing performed: Release package build passed; extracted package install made the Docker visible in CorelDRAW 27.1.0.129; package uninstall removed it. A subsequent fresh-launch visibility query returned true, but rendered/functioning automatic startup was not confirmed.

Follow-up: Resolve whether the fresh-launch state is supported CorelDRAW workspace persistence or an official addon mechanism before claiming automatic startup.

## 2026-09-16: Task 4 Badge Generator

Task: Implement generic badge planning and the first CorelDRAW generation adapter.

Reason: Start the first product feature while preserving the verified WPF/host boundary.

Files/areas affected: `CorelMate.Badges`, `CorelMate.Host`, `CorelMate.UI`, tests, and documentation.

What changed: Added strict generic placeholders, dictionary-backed data rows, millimeter layout/page planning, recursive grouped-shape inspection, native duplicate/move, Corel text replacement, page creation, command grouping, and manual WPF controls.

Important decisions: Keep calculations pure; use native Corel object-model duplication instead of clipboard; preserve the selected master; use Corel's text replacement API for formatting preservation.

Testing performed: Pure smoke tests passed. Real CorelDRAW 27.1 temporary-document test generated three copies from a grouped master with two placeholders.

Follow-up: Test the WPF workflow interactively, overflow pages, undo/redo, formatting preservation, and rerun release packaging with all managed dependencies.

## 2026-09-16: Task 5 Workflow Hardening

Task: Harden the real Badge Generator user workflow.

Reason: Task 4 had a functional engine but only a pipe-delimited input area and no preview/reset/busy-state protection.

Files/areas affected: `CorelMate.UI`, `CorelMate.Host`, and Task 5 documentation.

What changed: Added dynamic variable-derived row editors, row-level validation, preview stats, reset, friendly error mapping, duplicate-submit protection, and captured-document validation.

Important decisions: Keep Corel mutations synchronous and preview pure; do not introduce MVVM/dependency frameworks or background COM work.

Testing performed: Build/tests passed. Real CorelDRAW 27.1 generated 100 badges over 5 pages in about 9 seconds; missing-value preflight rejected; grouped generation undo/redo restored shape counts.

Follow-up: Manually verify the WPF buttons and formatting/large-quantity behavior in a foreground CorelDRAW session.

## 2026-09-16: Task 6 Convert Text to Curves

Task: Implement selection-scoped Convert Text to Curves.

Reason: Add the second major CorelMate feature using CorelDRAW's native object model without flattening unrelated artwork.

Files/areas affected: `CorelMate.Curves`, `CorelMate.Host`, `CorelMate.UI`, tests, packaging, and documentation.

What changed: Added shared planned traversal, preflight summary, locked/hidden classification, native `Shape.ConvertToCurves()`, command grouping, WPF confirmation/result controls, and Curves regression coverage.

Testing performed: Build/tests passed. CorelDRAW 27.1 verified grouped artistic conversion, paragraph conversion, non-text preservation, locked/hidden counts, and Undo/Redo.

Follow-up: Verify WPF confirmation clicks and special containers/text-on-path before claiming full feature coverage.

## 2026-09-16: Task 7 CSV/XLSX Import

Task: Add local spreadsheet import to Badge Generator without creating a second generation pipeline.

Reason: Allow CSV/XLSX data to feed the existing validated `BadgeDataRow` and CorelDRAW generation path.

Files/areas affected: `CorelMate.Badges`, `CorelMate.UI`, tests, packaging documentation, and agent memory.

What changed: Added standards-aware CSV parsing, direct first-worksheet XLSX ZIP/XML reading, header normalization/mapping, duplicate/missing/extra-column handling, row/quantity validation, Unicode support, and WPF file import.

Testing performed: Synthetic CSV/XLSX import tests and full solution regression tests pass. No Excel COM or clipboard automation was used.

Follow-up: Perform foreground CorelDRAW file-picker/import and imported-data generation verification.

## 2026-10-05: Task 9 Modern UI Design System

Task: Modernize the CorelMate WPF Docker UI with an Adaptive Pro design system and segmented tabbed layout.

Reason: Transform the initial prototype UI into a visually polished, professional, and intuitive user experience matching modern CorelDRAW standards.

Files/areas affected: `src/CorelMate.UI/Themes/CorelMateTheme.xaml`, `src/CorelMate.UI/CorelMatePanel.xaml`, `src/CorelMate.UI/CorelMatePanel.cs`, `src/CorelMate.UI/CorelMate.UI.csproj`, and agent documentation.

What changed:
- Created an Adaptive Pro WPF theme resource dictionary with modern typography, dark slate surfaces, emerald gradients, card borders, and refined control templates.
- Added segmented tab navigation for Badges (`🏷️ Badges`), Text to Curves (`🔤 Curves`), and System Info (`⚙️ Info`).
- Added live host connection indicator pill with color-coded status dot.
- Added dynamic rounded variable pill badges (`{{VARIABLE}}`) in the master capture card.
- Implemented a 4-tile live layout metrics grid (`TOTAL`, `COLUMNS`, `PER PAGE`, `PAGES`).
- Restyled data entry table, layout form inputs, and buttons with primary/secondary hierarchy and interactive hover states.

Testing performed: Release solution build passed with 0 warnings/errors; smoke tests passed; release packaging succeeded.

## 2026-11-17: Reference Extension Reverse Engineering & Page-To-Curves Implementation

Task: Reverse-engineer reference extension `CdrPreflight Installer X7-2020` and implement a native, version-compatible Page-to-Curves, All-Pages-to-Curves, and Selection-to-Curves feature in CorelMate.

Reason: Fulfill user requirement to evaluate reference extension architecture, understand its curve conversion mechanisms, address its shortcomings, and build an independent, robust, non-destructive text-to-curves solution with document preservation.

Files/areas affected: `src/CorelMate.Curves/ConvertTextToCurvesService.cs`, `src/CorelMate.Host/CorelShapeTraversal.cs`, `src/CorelMate.Host/CorelTextToCurvesConverter.cs`, `src/CorelMate.Host/CorelMenuIntegration.cs`, `src/CorelMate.UI/CorelMatePanel.xaml`, `src/CorelMate.UI/CorelMatePanel.cs`, `tests/CorelMate.Tests/Program.cs`, `.agent-memory/`, `CHANGELOG.md`.

What changed:
- Extracted and reverse-engineered `CdrPreflight Installer X7-2020` (NSIS-3 extraction, MS-OVBA decompression of 11 VBA modules).
- Analyzed technical structure: IE MSHTML browser docker, GMS automation, recursive `s.Shapes.All` and `s.PowerClip.Shapes.All` traversal, `BeginCommandGroup` + optimization flag boosting.
- Enhanced `CorelMate.Curves`: Added `CurveConversionScope` (`SelectedObjects`, `CurrentPage`, `AllPages`), `CurveConversionOptions`, and extended `CurveConversionSummary` with detailed preflight/execution metrics.
- Enhanced `CorelMate.Host`: Upgraded `CorelShapeTraversal` to support Page and Document traversal, recursive group preservation without destructive ungrouping, PowerClip inspection, and layer lock/visibility tracking.
- Upgraded `CorelTextToCurvesConverter`: Added multi-scope preflight, document copy preservation (`SaveAsCopy`/`File.Copy` with `_curves.cdr` suffix), atomic undo command grouping, performance optimization toggling, and verification.
- Updated `CorelMenuIntegration`: Added native menu commands in `UserDraw.xslt` and companion macro subs in `CorelMate.gms` (`ConvertPageToCurves`, `ConvertAllPagesToCurves`, `ConvertSelectionToCurves`).
- Updated `CorelMate.UI`: Redesigned Curves tab with target scope selector, backup copy checkbox, preflight analysis breakdown, and conversion results.
- Expanded `CorelMate.Tests`: Added tests for scope resolution, preflight models, options, and summary metrics.

Testing performed: Solution build passed (0 warnings, 0 errors); smoke test suite passed; release packaging succeeded.

## 2026-11-17: Official Release CorelMate v1.18.1

Task: Package and publish the official production release CorelMate v1.18.1.

Reason: User request to establish and publish the latest official release designated as `CorelMate v1.18.1`.

Files/areas affected: `Directory.Build.props`, `scripts/Build-Release.ps1`, `src/CorelMate.UI/CorelMatePanel.xaml`, `README.md`, `CHANGELOG.md`, `.agent-memory/CURRENT_STATE.md`, `.agent-memory/TASK_HISTORY.md`.

What changed:
- Promoted solution version from 0.2.0 to 1.18.1 in `Directory.Build.props`.
- Updated release build script `Build-Release.ps1` to produce `CorelMate-1.18.1-docker.zip`.
- Updated WPF Docker UI version label to `1.18.1 (Production Release)`.
- Updated documentation and changelog with full release notes.

Testing performed: Solution build passed (0 warnings, 0 errors); smoke tests passed; release distribution package created and validated.

## 2026-11-17: Native GMS Execution Pipeline & Prohibited Icon Elimination

Task: Fix menu actions failing with "red loading spinner / prohibited icon and nothing happens" when clicking CorelMate navbar items.

Reason: User reported clicking navbar items showed a red loading/prohibited icon and nothing happened.

Files/areas affected: `src/CorelMate.Core/CorelWorkspacePatcher.cs`, `src/CorelMate.Host/CorelMenuIntegration.cs`, `tests/CorelMate.Tests/Program.cs`, `assets/gms/CorelMate.gms`, `assets/vba/CorelMate.bas`, `scripts/Install-Dev.ps1`, `packaging/Install-CorelMate.ps1`, `CHANGELOG.md`, `.agent-memory/CURRENT_STATE.md`, `.agent-memory/TASK_HISTORY.md`.

What changed:
- Diagnosed root cause: CorelDRAW's `dynamicCategory="2cc24a3e-fe24-4708-9a74-9c75406eebcd"` exclusively resolves macros indexed by `GMSManager` from `.gms` files in `Draw\GMS\`. CorelDRAW displayed the default unresolved macro icon (a hand with a red circle, interpreted as a "red load icon") and disabled execution.
- Generated and verified precompiled `CorelMate.gms` (Project: `CorelMate`, Module: `Workflows`) deploying all 7 macros directly into `Draw\GMS\`.
- Re-anchored dynamic commands across `CorelWorkspacePatcher.cs`, `CorelMenuIntegration.cs`, and `UserDraw.xslt` to `CorelMate.Workflows.*`.
- Removed conflicting `type="button"` attribute from macro `itemData` definitions.
- Hardened `Install-Dev.ps1` and `Install-CorelMate.ps1` to deploy `assets/gms/CorelMate.gms` directly into user AppData `Draw\GMS`.
- Updated smoke tests in `tests/CorelMate.Tests/Program.cs`.

Testing performed: Solution Release build passed (0 warnings, 0 errors); CorelMate.Tests smoke tests passed; verified fresh CorelDRAW session indexing of all 7 macros under `CorelMate.Workflows.*` in `GMSManager.Projects`; deployed via `Install-Dev.ps1`.
