# Task History

## 2026-09-16: Initial Agent Bootstrap

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