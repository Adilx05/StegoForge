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

## Security and misuse boundaries

StegoForge is intended for legitimate uses such as watermarking, data provenance, secure archival workflows, and educational research.

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
