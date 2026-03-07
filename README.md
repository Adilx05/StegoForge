# StegoForge

StegoForge is a modular .NET steganography platform intended to provide reliable, testable, and extensible tooling for embedding and extracting payloads from carrier files. The repository currently contains the solution scaffolding, domain contracts, baseline applications (CLI/WPF), and test projects that establish the architecture for future feature delivery.

## Project overview

The solution is organized around clear boundaries:

- **Core contracts (`StegoForge.Core`)**: shared models, service abstractions, and error contracts.
- **Application layer (`StegoForge.Application`)**: orchestration layer for use cases (planned implementation).
- **Infrastructure/services**: crypto, compression, formats, and infrastructure modules that will implement core interfaces.
- **Delivery apps**: CLI (`StegoForge.Cli`) and desktop GUI (`StegoForge.Wpf`).
- **Tests**: unit, integration, CLI, and WPF test projects.

This structure enables new formats/providers to be added without coupling transport/UI logic to steganography internals.

## Supported formats (planned)

Planned carrier formats are tracked in `docs/roadmap.md`; initial priority targets include:

- PNG (lossless image carriers)
- BMP (simple baseline image carriers)
- WAV (PCM audio carriers)
- Optional future expansion: WebP (lossless mode), FLAC, and containerized formats where deterministic byte-level placement is feasible.

Planned payload packaging capabilities include metadata framing, encryption, integrity checks, and optional compression.

## Architecture summary

A layered architecture is used:

1. **UI/Entry points** (CLI/WPF) gather user input and display results.
2. **Application services** coordinate validation and call domain abstractions.
3. **Core abstractions/models** define embed/extract/capacity/info contracts.
4. **Provider implementations** (formats/crypto/compression/infrastructure) perform actual byte manipulation and cryptographic operations.

See:

- `docs/architecture.md` for component-level details.
- `docs/payload-format.md` for planned payload envelope shape and versioning.

## Build & test quickstart

Prerequisites:

- .NET SDK 9.0+ (as pinned in `global.json`)
- Windows recommended for WPF builds/tests

Common commands:

```bash
# Restore and build all projects
 dotnet build StegoForge.sln

# Run all tests
 dotnet test StegoForge.sln

# Run CLI (placeholder app)
 dotnet run --project src/StegoForge.Cli
```

For full build matrix examples (CLI-only, WPF-only, full solution), see `docs/building.md`.


## CLI usage and diagnostics quick reference

Use `--quiet` for minimal user-facing output, `--verbose` for expanded operational detail, and `--json` for machine-readable diagnostics/error contracts.

```bash
# Safe default text diagnostics (human-readable, sanitized)
 dotnet run --project src/StegoForge.Cli -- embed --carrier in.png --payload secret.bin --out out.png

# Quiet mode for automation logs (errors only)
 dotnet run --project src/StegoForge.Cli -- extract --carrier out.png --out recovered.bin --quiet

# Verbose + JSON mode for deterministic machine parsing
 dotnet run --project src/StegoForge.Cli -- info --carrier out.png --verbose --json
```

Malformed/corrupted input handling is deterministic: failures map to stable `StegoErrorCode` values and predictable process exit codes, so scripts can branch reliably on malformed header/payload conditions (for example, `InvalidHeader` vs `InvalidPayload`).

For command-contract details, including error-shape expectations, see `docs/testing.md` (CLI matrix + hardening guidance) and `docs/architecture.md` (error contract + diagnostics policy).

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

Cross-reference: `docs/testing.md#ci-hardening-strategy`, `docs/testing.md#hardening-and-cancellation-test-guidance`, `docs/architecture.md#processing-hardening-limits`, and `docs/architecture.md#security-logging-policy`.

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

### Design guardrails (planned)

- Deterministic error contracts and explicit failure modes.
- Integrity verification for extracted payloads.
- Versioned payload framing for safe backward compatibility handling.
- Audit-friendly command logs in CLI/UI workflows (where feasible).

See `docs/testing.md` and `docs/roadmap.md` for quality and governance milestones.

## Contributor hardening test workflow (local)

Run focused hardening checks before opening a PR, then run the broader suite when touching envelope/format/error-contract code paths.

```bash
# PR-suitable hardening subset (unit + integration, excludes long nightly fuzz campaign)
 dotnet test StegoForge.sln --filter "Category=Hardening&Campaign!=Fuzz-Full"

# CLI deterministic failure contract checks
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~ReturnsMappedFailureCode|FullyQualifiedName~ParserError"

# Optional nightly-equivalent fuzz segment (longer running)
 dotnet test StegoForge.sln --filter "Category=Hardening&Campaign=Fuzz-Full"
```

See `docs/testing.md` for CI trait routing/artifact guidance and `docs/architecture.md` for processing-limit + security-logging hardening contracts.

