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

## 2026-11-17: Active Workspace In-Place Patching & Docker Accessibility

Decision: Complement Addon XSLT deployment with direct in-place XML patching of active CorelDRAW workspaces (`_default.cdws` -> `content/workspace.xml`) and provide native `*Docker(...)` toggle bindings in the main menubar and Window menu.

Context: While Addon XSLT works on system-wide addon installs, CorelDRAW v27 defaults `AddDocker` captions to the C# UserControl class name (`CorelMate.UI.CorelMatePanel`) in the active workspace and only scans user AppData addons if registered in `<Addons>`. Closing the docker also hid it without an obvious UI entry point if the navbar had not yet updated.

Reason: Directly patching `_default.cdws` in user AppData:

1. Immediately fixes the docker title bar and right-side tab strip to display `CorelMate` instead of the C# class name.
2. Injects the `CorelMate` top-level navbar menu item directly next to `Help`.
3. Injects `CorelMate Docker` toggle commands into both `CorelMate` and `Window` menus using CorelDRAW's native `*Docker('{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}')` binding, providing instant toggle, live checkmarks, and closed-docker reopening without any VBA dependency.

Alternatives: Relying exclusively on XSLT merging or asking users to manually drag commands via Tools -> Customization.

Consequences: `CorelWorkspacePatcher` added to `CorelMate.Core` for pure XML and ZIP in-place manipulation, tested thoroughly by `CorelMate.Tests`. Installed automatically during developer and production installation scripts.

## ADR-011: Single-Instance COM Activation for In-Process Docker Connection

Status: Accepted.

Context: `Marshal.GetActiveObject("CorelDRAW.Application")` consistently failed with `MK_E_UNAVAILABLE` (0x800401E3) inside CorelDRAW because CorelDRAW does not register in the Windows Running Object Table (ROT). Consequently, the Docker initialized in an permanent "Offline" state where all actions refused to run.

Decision: Use `Type.GetTypeFromProgID("CorelDRAW.Application")` with `Activator.CreateInstance()`. Because CorelDRAW's COM class is a single-instance local server, `CreateInstance` connects directly to the existing running process without launching an extra instance. Also implement on-demand reconnection in all user action handlers and a deferred 1-second retry timer upon panel initialization.

Consequences: Docker connects reliably inside CorelDRAW, shows live connection status, displays active document metadata, and auto-reconnects on user clicks.

## ADR-012: Adaptive Light/Dark Theme Detection and Streamlined 3-Tab UI

Status: Accepted.

Context: CorelDRAW's workspace is typically used in Light Theme, but CorelMate previously hardcoded dark background and border brushes, causing visual contrast mismatch. Furthermore, fitting 6 tabs horizontally inside a 300px sidebar Docker caused extreme button crowding and visual clutter.

Decision: Create dedicated `LightTheme.xaml` and `DarkTheme.xaml` resource profiles and auto-detect system theme via `AppsUseLightTheme` in Windows registry. Provide a manual 1-click ☀️ / 🌙 theme toggle in the header. Consolidate the 6 tabs into 3 clean, spacious tabs: Curves (primary 1-click workflow), Badges, and Tools & Info.

Consequences: Docker UI matches CorelDRAW's native look and feel, eliminates visual clutter, and provides an immediate 1-click text conversion workflow.
