# Decisions

## 2026-09-16: Preserve The Existing Modular Boundary

Decision: Keep pure modules separate from `CorelMate.Host` and `CorelMate.UI`.

Context: CorelDRAW COM types must not leak through badge, AI, curves, or infrastructure logic.

Reason: It keeps pure logic testable and isolates host/runtime compatibility.

Alternatives: One project or feature projects referencing COM directly.

Consequences: Host adapters may require explicit mapping code, but feature modules remain portable.

## 2026-09-16: WPF Docker Surface

Decision: Use a public parameterless WPF `UserControl` targeting .NET Framework 4.8 for the v27 Docker.

Context: CorelDRAW v27.1 accepted and rendered WPF through `FrameWork.AddDocker`; WinForms controls failed at `ShowDocker`.

Reason: Verified real-host compatibility.

Alternatives: WinForms, HTML/JavaScript, native addon packaging.

Consequences: WPF/Corel host compatibility is a required validation boundary.

## 2026-09-16: Mandatory PR-Only Changes

Decision: Never change `main` directly. Every change must use a pushed branch and a GitHub PR created and merged with `gh` or the GitHub UI.

Context: The repository is now published at `https://github.com/logicbyroshan/CorelMate.git` and the owner requested a reviewable history.

Reason: Preserve reviewability, checks, and a clean protected-main workflow.

Alternatives: Direct main commits or local-only changes.

Consequences: Every future task must include branch creation, push, PR, checks/review, merge, and cleanup.

## 2026-11-17: Version Detection & Multi-Era UI Profiles

Decision: Establish a central CorelCompatibilityRegistry in `CorelMate.Core` supporting CorelDRAW 2017–2026 (v19–v28), and provide 5 authentic version design profiles in `CorelMate.UI/Themes`.

Context: Users operate multiple CorelDRAW versions from 2017 to 2026. A generic modern UI looks out of place in older versions, and hardcoding v27 blocked older supported suites.

Reason: Delivers authentic native look-and-feel across all supported versions while maintaining a single shared codebase and testing suite.

Alternatives: Separate binaries per version or single static theme.

Consequences: Dynamic theme merging via `CorelMateThemeManager`; UI tokens must use DynamicResource.

## 2026-11-17: Native Menu Integration Strategy

Decision: Combine native Addon XSLT stylesheet (`UserDraw.xslt` + `Coreldrw.addon`) with companion GMS macro (`CorelMate.gms`) and COM Docker automation.

Context: CorelMate needed to appear as the last main navigation menu item in CorelDRAW without disrupting native menus or disappearing on restart.

Reason: CorelDRAW's native framework natively parses Addon XSLT transforms during menu bar assembly (`f3016f3c-2847-4557-b61a-a2d05319cf18`). Deploying to `%APPDATA%\Corel\<Suite>\Draw\Addons` allows non-elevated installation and restart persistence.

Alternatives: Pure Win32 hooking or manual workspace editing.

Consequences: Clean uninstall requires removing both the Addon directory and the GMS companion macro.

## 2026-11-17: Page-to-Curves Architecture & Reference Reverse Engineering

Decision: Adopt recursive in-place shape traversal and host optimization patterns observed from reverse-engineering `CdrPreflight`, but improve safety with non-destructive group preservation, document copy preservation (`SaveAsCopy`/`File.Copy` with `_curves.cdr`), atomic undo command grouping, and WPF-based multi-scope preflight.

Context: Users require converting text to curves across whole pages, selections, or full documents for prepress/preflight while preserving layout hierarchy and original document copies. CdrPreflight showed effective optimization flags (`Optimization = True`, `EventsEnabled = False`) but used obsolete IE browser dockers, lacked document backups, and swallowed errors silently.

Reason: Non-destructive in-place conversion preserves groups, PowerClips, and text-on-path without destructive ungrouping. Document copy preservation prevents accidental irreversible data loss.

Alternatives: Blind ungrouping (breaks visual effects/clipping), rasterization (destroys vector scalability), or external PDF conversion.

Consequences: COM calls remain strictly isolated in `CorelMate.Host`; `CorelMate.Curves` holds pure preflight and summary contracts.
