<p align="center">
	<img src="assets/brand/corelmate-logo.png" alt="CorelMate logo" width="180">
</p>

<h1 align="center">CorelMate</h1>

<p align="center">A Windows-first productivity extension for CorelDRAW.</p>

<p align="center">
	<a href="https://github.com/logicbyroshan/CorelMate"><img src="https://img.shields.io/github/stars/logicbyroshan/CorelMate?style=flat" alt="GitHub stars"></a>
	<a href="https://github.com/logicbyroshan/CorelMate/releases"><img src="https://img.shields.io/github/v/release/logicbyroshan/CorelMate?include_prereleases&label=version" alt="Release version"></a>
	<a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="MIT license"></a>
	<a href="https://github.com/logicbyroshan/CorelMate/issues"><img src="https://img.shields.io/github/issues/logicbyroshan/CorelMate" alt="Open issues"></a>
</p>

CorelMate adds focused workflow tools to CorelDRAW while keeping CorelDRAW as the host application. It is built around a modular .NET architecture, a native-feeling WPF Docker, and a narrow CorelDRAW Object Model boundary.

## Current Status

CorelMate is an active pre-1.0 open-source project targeting CorelDRAW Graphics Suite 2026, internal version 27.x.

Currently available:

- WPF Docker hosted through CorelDRAW's `FrameWork.AddDocker` API.
- Generic Badge Generator with `{{VARIABLE}}` placeholders, dynamic rows, quantities, millimeter layout, preview, and multi-page generation.
- Native Convert Text to Curves workflow for selected artistic and paragraph text.
- Local CSV and XLSX import mapped into the same normalized Badge Generator rows as manual entry.
- Host-independent smoke tests and a reproducible release ZIP workflow.

Still limited or unverified:

- Automatic `.addon` startup registration is not verified for .NET assemblies on v27.1.
- Full click-by-click Docker UI validation requires a foreground CorelDRAW session.
- Special containers, symbols, and text-on-path behavior need further testing.
- AI, licensing, telemetry, and installer redesign are not part of the current release.

Do not use the pre-1.0 build for destructive artwork workflows without testing against a copy of the document.

## Features

### Badge Generator

Create one master badge in CorelDRAW, select it, and generate copies from manually entered data rows. The engine is generic and does not contain school-specific fields.

- Detects placeholders such as `{{TITLE}}` and `{{STANDARD}}` recursively through groups.
- Preserves the selected master by duplicating it through CorelDRAW's object model.
- Supports quantities, gaps, margins, page fitting, and additional pages.
- Keeps calculations in millimeters at the application boundary.
- Uses CorelDRAW command grouping so generation can be undone as one operation where supported.

### Convert Text to Curves

Preflights the current selection, reports convertible/locked/hidden text, asks for confirmation, and calls CorelDRAW's native `Shape.ConvertToCurves()` operation. Unrelated rectangles, images, and other vector objects are not flattened.

### CSV / XLSX Import

After capturing a master, choose **Import CSV / XLSX** in the Badge Generator. CorelMate reads the first worksheet for XLSX, detects headers case-insensitively, maps headers to detected placeholders, recognizes `QUANTITY` or `QTY`, reports unused columns, and validates the complete dataset before CorelDRAW is changed. CSV parsing supports quoted commas, escaped quotes, multiline fields, UTF-8, CRLF, and LF.

## Architecture

```text
CorelDRAW 2026
	|
	v
WPF CorelMate Docker
	|
	v
CorelMate.UI -> CorelMate.Host
					|-> CorelMate.Badges
					|-> CorelMate.Curves
					|-> CorelMate.Core
					|-> CorelMate.Infrastructure
					`-> CorelMate.AI (abstraction only)
```

| Project | Responsibility |
|---|---|
| `CorelMate.Core` | Shared settings and domain concepts |
| `CorelMate.Badges` | Pure placeholders, data rows, layout, and page planning |
| `CorelMate.Curves` | Pure conversion summaries and Curves contracts |
| `CorelMate.Host` | CorelDRAW/VGCore COM adapter and document mutation |
| `CorelMate.UI` | WPF Docker and user workflows |
| `CorelMate.Infrastructure` | Logging and infrastructure seams |
| `CorelMate.AI` | Future provider abstraction; no network implementation |
| `CorelMate.Tests` | Dependency-free executable smoke tests |

## Requirements

- Windows 10/11 x64
- CorelDRAW Graphics Suite 2026, v27.1.0.129 tested locally
- .NET Framework 4.8 Developer Pack
- .NET SDK 10.0.103 or compatible SDK
- PowerShell 5.1 or later
- CorelDRAW installed for host integration tests

The host project references Corel's installed assemblies through a machine-specific SDK path. Recheck `src/CorelMate.Host/CorelMate.Host.csproj` on another machine before building.

## Build And Test

```powershell
dotnet build .\CorelMate.sln -c Release
dotnet run --project .\tests\CorelMate.Tests\CorelMate.Tests.csproj -c Release
```

The smoke test covers pure placeholder, quantity, layout, and conversion-summary behavior. It does not replace a real CorelDRAW integration test.

Import parsers and mapping are covered with synthetic CSV/XLSX data, including Unicode and invalid-data cases. No real personal/student datasets are stored in the repository.

## Development Docker

```powershell
dotnet build .\CorelMate.sln -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\Install-Dev.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Uninstall-Dev.ps1
```

These scripts use COM automation and do not modify CorelDRAW installation files. Close CorelDRAW before rebuilding if it has loaded a Docker assembly and MSBuild reports a file lock.

## Release Package

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1
```

This creates `artifacts\CorelMate-1.18.1-docker.zip`. The package contains the WPF Docker and required managed CorelMate assemblies, plus package-local session install/remove scripts and a README. It excludes source, tests, PDBs, secrets, local paths, and Corel-owned interop assemblies.

Extract the ZIP and run `Install-CorelMate.ps1` or `Uninstall-CorelMate.ps1`. Automatic `.addon` startup is explicitly not claimed because the supported .NET startup mechanism is not verified for v27.1.

## Documentation

- [Development guide and agent rules](AGENTS.md)
- [Contribution guide](CONTRIBUTING.md)
- [CorelDRAW integration research](.agents/CORELDRAW_INTEGRATION.md)
- [Architecture](.agents/ARCHITECTURE.md)
- [Testing strategy and results](.agents/TESTING.md)
- [Roadmap](.agents/ROADMAP.md)
- [Known limitations](.agents/KNOWN_LIMITATIONS.md)
- [Project decisions](.agents/DECISIONS.md)
- [Change history](CHANGELOG.md)

Read the relevant `.agents/` and `.agent-memory/` files before making changes.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) first. Never push directly to `main`; use a focused branch, push it, open a PR with `gh pr create`, pass checks/review, and merge through the PR.

## Security

Never commit API keys, credentials, tokens, private keys, local configuration, or user artwork. Report security concerns privately as described in [SECURITY.md](SECURITY.md).

## License

CorelMate is released under the [MIT License](LICENSE). CorelDRAW and its interop assemblies remain property of Corel Corporation; CorelMate does not redistribute Corel-owned interop assemblies.

## Links

- Repository: https://github.com/logicbyroshan/CorelMate
- Issues: https://github.com/logicbyroshan/CorelMate/issues
- Pull requests: https://github.com/logicbyroshan/CorelMate/pulls