# Building

_Last verified against source: 2026-03-07 (`0fd7c07`)._
## Prerequisites

- .NET SDK 10.0+ (see `global.json`)
- Git
- Windows required for WPF build/test workflows and full-solution test runs

## CLI-only (cross-platform) build/test

These commands match the `core-cli` CI job and run on Linux/macOS/Windows.

```bash
dotnet restore src/StegoForge.Core/StegoForge.Core.csproj
dotnet restore src/StegoForge.Application/StegoForge.Application.csproj
dotnet restore src/StegoForge.Cli/StegoForge.Cli.csproj
dotnet restore tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj
dotnet restore tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj
dotnet restore tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

dotnet build src/StegoForge.Core/StegoForge.Core.csproj --configuration Release --no-restore
dotnet build src/StegoForge.Application/StegoForge.Application.csproj --configuration Release --no-restore
dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release --no-restore

dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build
dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-build
dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release --no-build
```

## WPF-only (Windows-specific) build/test

> Run these only on Windows hosts with desktop workloads installed.

```powershell
dotnet restore src/StegoForge.Wpf/StegoForge.Wpf.csproj
dotnet restore tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj

dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj --configuration Release --no-restore
dotnet build tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-restore

dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build --filter "FullyQualifiedName~WpfCommandFlowTests"
```

## Full-solution build/test

> `StegoForge.sln` includes WPF projects. Build/test the full solution on Windows.

```powershell
dotnet restore StegoForge.sln
dotnet build StegoForge.sln --configuration Release
dotnet test StegoForge.sln --configuration Release
```

## Contributor shortcut: envelope-focused tests

When working on payload envelope framing, you can run only the envelope-related unit tests:

```bash
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --filter PayloadEnvelope
```

This filter matches `PayloadEnvelopeSerializerTests`/`PayloadEnvelopeContractsTests` and gives fast feedback before broader suite runs.


## Compression-focused test commands

When changing compression behavior, run these focused commands before broader suites:

```bash
# Compression provider unit tests (Deflate provider behavior and error mapping)
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --filter "FullyQualifiedName~CompressionProviderContractTests|FullyQualifiedName~DeflateCompressionProviderTests"

# Compression orchestration integration tests (policy + envelope metadata/flags)
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~CompressionOrchestrationIntegrationTests

# PNG-focused integration tests (v1 acceptance)
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~PngRoundTripIntegrationTests
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~CapacityServiceIntegrationTests
```

These commands specifically cover compression provider contract enforcement, compression-mode policy (`Disabled`/`Enabled`/`Automatic`), and deterministic decompression failure behavior.


## Application orchestration/policy-focused test commands

When changing service orchestration, policy validation, or deterministic error behavior, run these targeted commands first:

```bash
# Application-service DI + shared orchestration integration checks
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~ApplicationServiceOrchestrationIntegrationTests

# Cross-format orchestration consistency + fail-fast policy integration checks
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~OrchestrationConsistencyIntegrationTests

# Policy validator + resolver unit checks
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --filter "FullyQualifiedName~OperationPolicyValidatorTests|FullyQualifiedName~CarrierFormatResolverTests"
```

These commands cover shared DI/service orchestration wiring, fail-fast policy enforcement before handler I/O, deterministic resolver precedence, and cross-format consistency guarantees.


## BMP-focused test commands

When changing BMP handler behavior, run these targeted commands before broader suites:

```bash
# BMP unit tests (handler + capacity calculator)
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --filter "FullyQualifiedName~StegoForge.Tests.Unit.Bmp.BmpLsbFormatHandlerTests|FullyQualifiedName~StegoForge.Tests.Unit.Bmp.BmpLsbCapacityCalculatorTests"

# BMP integration tests (capacity service + round-trip flows)
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter "FullyQualifiedName~BmpCapacityServiceIntegrationTests|FullyQualifiedName~BmpRoundTripIntegrationTests"
```

These commands verify BMP capacity boundaries, deterministic unsupported-format behavior, and end-to-end embed/extract round-trip reliability.

## WAV-focused test commands

When changing WAV handler behavior, run these targeted commands before broader suites:

```bash
# WAV unit tests (handler, capacity calculator, and strict format validation)
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --filter "FullyQualifiedName~StegoForge.Tests.Unit.Wav.WavLsbFormatHandlerTests|FullyQualifiedName~StegoForge.Tests.Unit.Wav.WavLsbCapacityCalculatorTests|FullyQualifiedName~StegoForge.Tests.Unit.Wav.WavLsbFormatValidationTests"

# WAV integration tests (capacity service + end-to-end round-trip behavior)
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter "FullyQualifiedName~WavCapacityServiceIntegrationTests|FullyQualifiedName~WavRoundTripIntegrationTests"

# Optional: run only WAV round-trip integration acceptance set
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --filter FullyQualifiedName~WavRoundTripIntegrationTests
```

These commands verify deterministic capacity boundaries, WAV embed/extract round-trip reliability (baseline/compressed/encrypted), and strict unsupported-format/invalid-header handling.

## CI command mapping

| Documented local command | CI workflow job/step |
| --- | --- |
| `dotnet restore src/StegoForge.Core/StegoForge.Core.csproj` (and the other five core/CLI restores above) | `.github/workflows/ci.yml` → `core-cli` → `Restore core/CLI projects` |
| `dotnet build src/StegoForge.Core/StegoForge.Core.csproj --configuration Release --no-restore` (and the other five core/CLI builds above) | `.github/workflows/ci.yml` → `core-cli` → `Build core/CLI projects` |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build` (plus integration + CLI commands above) | `.github/workflows/ci.yml` → `core-cli` → `Test core/CLI projects` |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build --filter "Category=Hardening&Campaign!=Fuzz-Full"` and integration equivalent | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (bounded; PR/push)` |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build --filter "Category=Hardening&Campaign=Fuzz-Full"` and integration equivalent | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (full fuzz campaigns; nightly/scheduled)` |
| `dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-build --filter FullyQualifiedName~PngRoundTripIntegrationTests` (plus PNG/BMP acceptance filters) | `.github/workflows/ci.yml` → `core-cli` → `PNG/BMP v1 acceptance smoke filter (PR)` |
| `dotnet restore src/StegoForge.Wpf/StegoForge.Wpf.csproj` + `dotnet restore tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj` | `.github/workflows/ci.yml` → `wpf` → `Restore WPF projects` |
| `dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj --configuration Release --no-restore` + WPF tests build | `.github/workflows/ci.yml` → `wpf` → `Build WPF projects` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build` | `.github/workflows/ci.yml` → `wpf` → `Test WPF smoke project` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build --filter "FullyQualifiedName~WpfCommandFlowTests"` | `.github/workflows/ci.yml` → `wpf` → `Test WPF command-flow subset` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build --filter "Category=Hardening"` | `.github/workflows/ci.yml` → `wpf` → `Test WPF hardening subset (Windows)` |
| `dotnet publish src/StegoForge.Cli/StegoForge.Cli.csproj --configuration Release --output artifacts/cli` | `.github/workflows/release.yml` → `package-cli` → `Publish CLI` |
| `dotnet publish src/StegoForge.Wpf/StegoForge.Wpf.csproj --configuration Release --output artifacts/wpf` | `.github/workflows/release.yml` → `package-wpf` → `Publish WPF` |

## CI workflow behavior (`.github/workflows/ci.yml`)

- `core-cli` job runs on an OS matrix: `ubuntu-latest` and `windows-latest`.
- `core-cli` restores/builds/tests only cross-platform projects and test suites:
  - `StegoForge.Core`
  - `StegoForge.Application`
  - `StegoForge.Cli`
  - `StegoForge.Tests.Unit`
  - `StegoForge.Tests.Integration`
  - `StegoForge.Tests.Cli`
- WPF projects are intentionally excluded from `core-cli` to prevent non-Windows CI failures.
- `wpf` job runs only on `windows-latest` and handles:
  - `StegoForge.Wpf`
  - `StegoForge.Tests.Wpf`
- Both jobs upload `.trx` test result files as build artifacts:
  - `test-results-core-cli-ubuntu-latest`
  - `test-results-core-cli-windows-latest`
  - `test-results-wpf-windows-latest`

## Release workflow behavior (`.github/workflows/release.yml`)

- Trigger: manual `workflow_dispatch` with validated `tag`, `version`, and `changelog_summary` metadata.
- `package-cli` (Ubuntu) publishes CLI output and emits deterministic release files:
  - `stegoforge-cli-<tag>-linux-x64.tar.gz`
  - `stegoforge-cli-<tag>-linux-x64.sha256`
  - `stegoforge-cli-<tag>-linux-x64.tar.gz.sig`
  - `stegoforge-cli-<tag>-linux-x64.sha256.sig`
- `package-wpf` (Windows) publishes WPF output and emits deterministic release files:
  - `stegoforge-wpf-<tag>-windows-x64.zip`
  - `stegoforge-wpf-<tag>-windows-x64.sha256`
  - `stegoforge-wpf-<tag>-windows-x64.zip.sig`
  - `stegoforge-wpf-<tag>-windows-x64.sha256.sig`
- Both packaging jobs sign artifacts + checksum manifests with cosign and run pre-upload verification (`sha256` + `cosign verify-blob`).
- `publish-release` downloads the packaged assets and publishes them as GitHub Release attachments.

## Useful diagnostics

```bash
# Build with normal verbosity for troubleshooting
 dotnet build StegoForge.sln -v normal

# List installed SDKs
 dotnet --list-sdks
```

## Notes

- If WPF targets fail on non-Windows hosts, build/test CLI/core/integration projects separately.
- Keep local SDK aligned with `global.json` to avoid restore/build drift.


## Release process

StegoForge release operations follow SemVer and are executed via `.github/workflows/release.yml` using explicit metadata inputs.

### Version bump rules (SemVer policy)

- **MAJOR (`X.0.0`)**: breaking changes to public contracts/compatibility guarantees.
- **MINOR (`X.Y.0`)**: backward-compatible feature additions.
- **PATCH (`X.Y.Z`)**: backward-compatible fixes, hardening updates, and docs-only maintenance releases.

### Required tag format

- Tags must match `vX.Y.Z`.
- Workflow input validation enforces:
  - `tag` matches `^v\d+\.\d+\.\d+$`
  - `version` matches `^\d+\.\d+\.\d+$`
  - `tag == v{version}`


### Operator runbook (release cut)

1. **Create and push the release tag** (after checklist completion and final `CHANGELOG.md` review):

```bash
git checkout main
git pull --ff-only
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

2. **Dispatch and monitor the release workflow** in `.github/workflows/release.yml`:

- Open GitHub Actions → **Release** workflow.
- Run workflow with:
  - `tag`: `vX.Y.Z`
  - `version`: `X.Y.Z`
  - `changelog_summary`: summary that matches `CHANGELOG.md`.
- Confirm jobs complete successfully in order:
  - `package-cli`
  - `package-wpf`
  - `publish-release`

3. **Validate uploaded artifacts and signatures** from the published release:

- Verify required assets exist:
  - `stegoforge-cli-vX.Y.Z-linux-x64.tar.gz`
  - `stegoforge-cli-vX.Y.Z-linux-x64.sha256`
  - `stegoforge-cli-vX.Y.Z-linux-x64.tar.gz.sig`
  - `stegoforge-cli-vX.Y.Z-linux-x64.sha256.sig`
  - `stegoforge-wpf-vX.Y.Z-windows-x64.zip`
  - `stegoforge-wpf-vX.Y.Z-windows-x64.sha256`
  - `stegoforge-wpf-vX.Y.Z-windows-x64.zip.sig`
  - `stegoforge-wpf-vX.Y.Z-windows-x64.sha256.sig`
  - `stegoforge-cosign.pub`
- Run checksum verification commands and cosign verification commands from [Artifact verification](#artifact-verification-consumer-workflow).
- Record release evidence: workflow run URL, successful verification command output, and asset list snapshot in release operations notes.

### Artifact verification (consumer workflow)

Download release assets for the target tag, including `stegoforge-cosign.pub`, then verify checksums and signatures before extraction/execution.

Checksum verification:

```bash
sha256sum --check stegoforge-cli-vX.Y.Z-linux-x64.sha256
```

```powershell
$line = Get-Content .\stegoforge-wpf-vX.Y.Z-windows-x64.sha256 -Raw
$parts = $line.Split('*', 2)
$expected = $parts[0].Trim().ToLowerInvariant()
$actual = (Get-FileHash -Path $parts[1].Trim() -Algorithm SHA256).Hash.ToLowerInvariant()
if ($expected -ne $actual) { throw "Checksum verification failed." }
```

Signature verification:

```bash
cosign verify-blob --key stegoforge-cosign.pub --signature stegoforge-cli-vX.Y.Z-linux-x64.tar.gz.sig stegoforge-cli-vX.Y.Z-linux-x64.tar.gz
cosign verify-blob --key stegoforge-cosign.pub --signature stegoforge-cli-vX.Y.Z-linux-x64.sha256.sig stegoforge-cli-vX.Y.Z-linux-x64.sha256
```

```powershell
cosign verify-blob --key .\stegoforge-cosign.pub --signature .\stegoforge-wpf-vX.Y.Z-windows-x64.zip.sig .\stegoforge-wpf-vX.Y.Z-windows-x64.zip
cosign verify-blob --key .\stegoforge-cosign.pub --signature .\stegoforge-wpf-vX.Y.Z-windows-x64.sha256.sig .\stegoforge-wpf-vX.Y.Z-windows-x64.sha256
```

### Trust anchors and key rotation

- Trust anchor: `stegoforge-cosign.pub` shipped with release assets.
- Operators should pin and compare the expected public-key fingerprint from release notes before accepting a new key.
- Rotation policy: publish old+new keys for one overlap release, sign with both during overlap when feasible, then retire old key in the next release notes.
- Emergency revocation: invalidate revoked key fingerprints immediately and accept only artifacts signed with the replacement key.

### Platform-specific signing limits

- Optional Windows Authenticode signing (if certificate secrets are configured) applies to Windows executable binaries only; it is not a cross-platform attestation mechanism.
- Cross-platform integrity/authenticity expectations are enforced via SHA-256 manifests and cosign detached signatures over both archives and checksum files.

### Changelog/release metadata requirements

Before dispatching a release:

1. Update `CHANGELOG.md` with a section for the release version.
2. Add categorized release notes and migration notes (or `None`).
3. Provide a non-empty `changelog_summary` workflow input.

The workflow fails fast if any required release metadata is missing or if `CHANGELOG.md` does not contain the requested version heading.


## Versioning during builds

The repository uses MinVer via `Directory.Build.props` for all projects (CLI, WPF, libraries, tests).

- Tag prefix: `v`
- Default local prerelease identifiers: `alpha.0`
- Shared metadata mapping:
  - `Version = $(MinVerVersion)`
  - `AssemblyVersion = $(MinVerMajor).$(MinVerMinor).0.0`
  - `FileVersion = $(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0`
  - `InformationalVersion = $(MinVerVersion)`

A shared MSBuild target (`Directory.Build.targets`) prints resolved values during build so version resolution is visible in local and CI logs.

Validation command:

```bash
dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj --configuration Release
```

Look for a log line beginning with `StegoForge version metadata:`.
