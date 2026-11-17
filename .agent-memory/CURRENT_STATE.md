# Current State

Updated 2026-09-16 during bootstrap.

- Repository contains the initial CorelMate 0.1.0 solution and existing `.agents` documentation.
- CorelDRAW 2026/v27.1.0.129 WPF Docker surface was previously verified manually through COM.
- Release build and the dependency-free badge-grid smoke test previously passed.
- No commits existed at bootstrap start; current branch was `main`; no Git remote was configured. This was resolved by PR #1.
- GitHub CLI is installed/authenticated as `logicbyroshan`.
- Bootstrap files were published through PR #1 from `chore/bootstrap-agent-memory` and are now merged into `main`.
- Automatic `.addon` startup registration remains unverified.
- Task 3 produced and real-host-tested a session-install ZIP; a fresh-launch visibility query returned true after removal, but rendered/functioning automatic startup was not confirmed and may reflect persisted CorelDRAW workspace state.
- Badge generation, curves, AI, production installer, licensing, and secure credential storage are unfinished.
- Task 4 now has a pure badge engine and initial CorelDRAW adapter; complete interactive UI validation remains pending.
- The direct adapter passed a real v27.1 temporary-document generation test; the WPF button-driven workflow and visual screenshot remain unverified.
- Task 5 added dynamic WPF rows, preview, reset, validation, and busy-state protection; 100-badge and undo/redo adapter tests passed.
- Task 6 adds native Convert Text to Curves through shared Host traversal; real artistic, paragraph, locked/hidden, and undo/redo checks passed.
- Task 7 adds dependency-free CSV/XLSX import mapped into existing BadgeDataRow values; synthetic parser and validation tests pass.
- Task 8 audited production state handling and fixed stale UI rows/results after failed capture/import/reset; real v27.1 manual adapter QA passed for 11 badges, invalid preflight, and undo/redo.
- Task 9 modernized the WPF UI with an Adaptive Pro design system, segmented tab navigation (Badges, Curves, Info), variable chip tags, live metric tiles, polished card surfaces, and styled input controls.
- CorelMate 0.2.0 engineering audit, redesign, and version adaptation completed:
  - Central Compatibility Registry explicitly handles CorelDRAW 2017 to 2026 (v19 to v28), rejecting < v19 as legacy.
  - Native CorelDRAW navigation menu integration deployed via User AppData / Programs Addons (`UserDraw.xslt` + `Coreldrw.addon`) and companion macro (`CorelMate.gms`).
  - Version-adaptive UI with 5 authentic era design profiles (2017 Classic, 2019 Charcoal, 2021 Teal, 2023 Azure, 2026 Adaptive Pro).
  - Document & Object Inspector implemented and integrated into the WPF panel.
  - Multi-version installer and uninstaller supporting non-elevated user installations.
  - Reverse-engineered reference extension `CdrPreflight Installer X7-2020` (extracted NSIS-3 installer, decompressed MS-OVBA VBA streams for `SanBonus`, `cp_Convert`, `ctc_boost`, `ctc_Convert_finde`).
  - Implemented Page-to-Curves, All-Pages-to-Curves, and Selection-to-Curves feature with document copy preservation (`_curves.cdr`), atomic undo grouping, host performance optimization, and non-destructive group/powerclip recursion.
  - Enhanced WPF Docker Curves tab with scope selector, preflight breakdown metrics, backup preservation toggle, and execution timing.
  - Added native CorelDRAW menu commands and companion macro subs for page/all-pages/selection curve conversion.
  - Full test suite passing across layout, parsing, import, metrics, version evaluation, and curve conversion models. Release package built and validated.