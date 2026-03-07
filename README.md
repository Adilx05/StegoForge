# StegoForge

_Last verified against source: 2026-03-07 (`0fd7c07`)._
StegoForge is a modular .NET steganography platform for reliable, testable embedding and extraction workflows across image and audio carriers. The repository currently includes working application services, production format handlers (PNG/BMP/WAV), a usable CLI surface, a usable WPF desktop workflow, and a multi-project test suite.

## Project overview

The solution is organized around clear boundaries:

- **Core contracts (`StegoForge.Core`)**: shared models, service abstractions, and error contracts.
- **Application layer (`StegoForge.Application`)**: orchestration services for embed/extract/capacity/info and policy validation.
- **Provider implementations**:
  - formats (`StegoForge.Formats`)
  - crypto (`StegoForge.Crypto`)
  - compression (`StegoForge.Compression`)
  - infrastructure (`StegoForge.Infrastructure`)
- **Delivery apps**: CLI (`StegoForge.Cli`) and desktop GUI (`StegoForge.Wpf`).
- **Tests**: unit, integration, CLI, and WPF test projects.

This structure enables new formats/providers to be added without coupling transport/UI logic to steganography internals.

## Supported formats (current)

Currently implemented and wired through embed/extract/capacity/info flows:

- PNG (`png-lsb-v1`, `PngLsbFormatHandler`)
- BMP (`bmp-lsb-v1`, `BmpLsbFormatHandler`)
- WAV (`wav-lsb-v1`, `WavLsbFormatHandler`)

These handlers are registered in `src/StegoForge.Formats/FormatServiceCollectionExtensions.cs` and covered by both unit and integration tests in `tests/StegoForge.Tests.Unit/` and `tests/StegoForge.Tests.Integration/`.

Future format expansion remains tracked in `docs/roadmap.md`.

## Architecture summary

A layered architecture is used:

1. **UI/Entry points** (CLI/WPF) gather user input and display results.
2. **Application services** validate requests and coordinate orchestration.
3. **Core abstractions/models** define embed/extract/capacity/info contracts.
4. **Provider implementations** (formats/crypto/compression/infrastructure) perform payload processing and carrier byte manipulation.

See:

- `docs/architecture.md` for component-level details.
- `docs/payload-format.md` for payload envelope shape and versioning.

## Build prerequisites

- .NET SDK **10.0.100** (pinned in `global.json`; CI uses `10.0.x` with prerelease enabled).
- Git.
- **Windows** is required to build/run WPF (`src/StegoForge.Wpf`) and WPF tests.

## Contributor quickstart

For full contribution workflow and policy requirements, see [CONTRIBUTING.md](CONTRIBUTING.md).

Run from repository root.

### CLI-only / Core (cross-platform)

This block is the same restore/build/test flow used by CI `core-cli`.

```bash
# Restore
dotnet restore src/StegoForge.Core/StegoForge.Core.csproj
dotnet restore src/StegoForge.Application/StegoForge.Application.csproj
dotnet restore src/StegoForge.Cli/StegoForge.Cli.csproj
dotnet restore tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj
dotnet restore tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj
dotnet restore tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

# Build
dotnet build src/StegoForge.Core/StegoForge.Core.csproj --configuration Release --no-restore
dotnet build src/StegoForge.Application/StegoForge.Application.csproj --configuration Release --no-restore
dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release --no-restore

# Test
dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-build
dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release --no-build
```

### WPF-only (Windows only)

This block is the same restore/build/test flow used by CI `wpf`.

```powershell
# Restore
dotnet restore src/StegoForge.Wpf/StegoForge.Wpf.csproj
dotnet restore tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj

# Build
dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-restore

# Test
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build
```

### Full solution (Windows only)

`StegoForge.sln` includes WPF projects/tests, so use this on Windows.

```powershell
dotnet restore StegoForge.sln
dotnet build StegoForge.sln --configuration Release
dotnet test StegoForge.sln --configuration Release
```

## CI/release command mapping

| Local command set | Workflow mapping |
| --- | --- |
| CLI/core restore/build/test block above | `.github/workflows/ci.yml` → `core-cli` (`Restore core/CLI projects`, `Build core/CLI projects`, `Test core/CLI projects`) |
| WPF restore/build/test block above | `.github/workflows/ci.yml` → `wpf` (`Restore WPF projects`, `Build WPF projects`, `Test WPF smoke project`) |
| `dotnet publish src/StegoForge.Cli/StegoForge.Cli.csproj --configuration Release --output artifacts/cli` | `.github/workflows/release.yml` → `package-cli` → `Publish CLI` |
| `dotnet publish src/StegoForge.Wpf/StegoForge.Wpf.csproj --configuration Release --output artifacts/wpf` | `.github/workflows/release.yml` → `package-wpf` → `Publish WPF` |

Environment caveats:

- WPF build/test/publish requires Windows.
- Full hardening fuzz campaigns (`Campaign=Fuzz-Full`) are intentionally scheduled-only in CI and may be long-running locally.


## Release process

StegoForge uses **Semantic Versioning (SemVer)** for all tagged releases.

### Version bump rules (SemVer policy)

- **MAJOR (`X.0.0`)**: increment when introducing breaking API/contract/behavior changes (including CLI contract breaks, payload format incompatibility, or incompatible defaults).
- **MINOR (`X.Y.0`)**: increment for backward-compatible features and additive capabilities.
- **PATCH (`X.Y.Z`)**: increment for backward-compatible bug fixes, hardening, and documentation-only release adjustments that do not change public contracts.

When uncertain between MINOR/PATCH, default to MINOR only if a user-observable capability is added; otherwise PATCH.

### Tag format

- Release tags **must** use the exact format `vX.Y.Z` (example: `v1.2.3`).
- The workflow validates the tag and version metadata and fails if `tag != v{version}`.

### Changelog expectations

- Every release must update `CHANGELOG.md` before tagging.
- Add a dedicated heading for the release version and date, with categorized bullets (Added/Changed/Fixed/Security as applicable).
- Include a **Migration notes** subsection for each release; if no action is required, explicitly state `None`.
- The release workflow validates that the release version has a corresponding section in `CHANGELOG.md` and that a changelog summary is provided as workflow metadata.

## CLI command status

Implemented root commands:

- `embed`
- `extract`
- `capacity`
- `info`
- `version`
- `help`

Quick examples:

```bash
# Embed payload into a carrier
dotnet run --project src/StegoForge.Cli -- embed --carrier in.png --payload secret.bin --out out.png

# Extract payload
dotnet run --project src/StegoForge.Cli -- extract --carrier out.png --out recovered.bin

# Estimate capacity and embeddability
dotnet run --project src/StegoForge.Cli -- capacity --carrier in.png --payload 1024

# Inspect carrier metadata
dotnet run --project src/StegoForge.Cli -- info --carrier out.png

# Print version/help
dotnet run --project src/StegoForge.Cli -- version
dotnet run --project src/StegoForge.Cli -- help
```

Diagnostics modes:

- `--quiet` for minimal user-facing output.
- `--verbose` for expanded operational detail.
- `--json` for machine-readable diagnostics/error contracts.

Malformed/corrupted input handling is deterministic: failures map to stable `StegoErrorCode` values and predictable process exit codes, so scripts can branch reliably on malformed header/payload conditions.

## WPF status (current)

**Usable now (Milestone 11):**

- Embed and Extract views are implemented and wired to application services.
- Validation, command enablement, progress lifecycle, and deterministic error mapping are covered by WPF tests.
- Windows CI restores/builds/tests the WPF app and test project.

**Still pending / not yet release-finalized:**

- Broader release-readiness items from Milestone 14 (release process hardening, packaging/signing verification guidance).
- Any future UX expansion beyond current embed/extract workflow remains roadmap-driven.

See `docs/gui.md` and `docs/roadmap.md` for detail.

## Current status by milestone

Aligned with `docs/roadmap.md` checklists:

| Milestone | Status summary |
| --- | --- |
| 1 — Solution scaffolding | ✅ Complete |
| 2 — Core contract finalization | ✅ Complete |
| 3 — Payload envelope v1 | ✅ Complete |
| 4 — Compression provider integration | ✅ Complete |
| 5 — Crypto provider integration | ✅ Complete |
| 6 — PNG format handler (v1) | ✅ Complete (`png-lsb-v1`) |
| 7 — BMP format handler | ✅ Complete (`bmp-lsb-v1`) |
| 8 — WAV format handler | ✅ Complete (`wav-lsb-v1`) |
| 9 — Application orchestration and policy rules | ✅ Complete |
| 10 — CLI command surface v1 | ✅ Complete |
| 11 — WPF GUI v1 | ✅ Complete |
| 12 — Hardening and robustness | ✅ Largely implemented and actively tested |
| 13 — Documentation and developer experience | 🚧 In progress |
| 14 — Release readiness (v1.0) | 🚧 In progress |

## Milestone 12: hardening and safe diagnostics

Milestone 12 extends quality gates from correctness into adversarial-resilience and operator-safe diagnostics.

### Fuzz and negative-path scope

- Serializer, payload-envelope, and carrier-handler fuzz campaigns include deterministic bounded PR coverage and deeper nightly campaigns.
- Negative-path coverage explicitly targets truncation, corrupted length fields, invalid flags, unsupported format combinations, and cancellation behavior.
- Hardening tests assert both typed exception class and mapped `StegoErrorCode` outcomes for deterministic failure semantics.

### Resource-usage limits and configurable safeguards

- Processing limits (`MaxPayloadBytes`, `MaxHeaderBytes`, `MaxEnvelopeBytes`, optional `MaxCarrierSizeBytes`) are treated as first-class safety controls, not optional afterthoughts.
- Limit checks occur before expensive allocation/decompression paths to reduce memory-pressure and denial-of-service risk from malformed carriers.
- Contributors should tune limits incrementally and keep CI defaults conservative; larger thresholds belong in dedicated stress/fuzz environments.

### Secret-safe diagnostics policy

- Diagnostics are sanitized by default, with operation context and correlation identifiers preserved for supportability.
- Sensitive material (passwords, plaintext payload bytes, derived keys) is always redacted from CLI/UI/log output.
- User-facing errors stay actionable but non-secret-bearing, and internal failures are surfaced via stable code/message contracts.

Cross-reference: [docs/testing.md#ci-hardening-strategy](docs/testing.md#ci-hardening-strategy), [docs/testing.md#hardening-and-cancellation-test-guidance](docs/testing.md#hardening-and-cancellation-test-guidance), [docs/architecture.md#processing-hardening-limits](docs/architecture.md#processing-hardening-limits), and [docs/architecture.md#security-logging-policy](docs/architecture.md#security-logging-policy).

## Security & misuse notes

StegoForge is intended strictly for legitimate uses such as watermarking, data provenance, secure archival workflows, and educational research. The project scope excludes malware concealment and unauthorized evasion activity.

### Allowed/expected use

- Protecting sensitive information under lawful authorization.
- Embedding traceable ownership/provenance markers.
- Internal testing and red-team exercises conducted with explicit permission.

### Prohibited/misuse scenarios

- Concealing malware, command-and-control instructions, or exfiltration payloads.
- Bypassing legal controls, policy controls, or forensic monitoring.
- Any unlawful surveillance, harassment, or unauthorized data access.

### Design guardrails

- Deterministic error contracts and explicit failure modes.
- Integrity verification for extracted payloads.
- Versioned payload framing for safe backward compatibility handling.
- Audit-friendly command logs in CLI/UI workflows (where feasible).

See `docs/testing.md` and `docs/roadmap.md` for quality and governance milestones.
