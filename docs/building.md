# Building

## Prerequisites

- .NET SDK 10.0+ (see `global.json`)
- Git
- Windows required for WPF build/test workflows

## Restore once

```bash
 dotnet restore StegoForge.sln
```

## CLI-only build/test

```bash
# Build CLI app
 dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj

# Run CLI tests
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

# Optional: run unit tests frequently with CLI work
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj
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

## WPF-only build/test

> Recommended on Windows with desktop workloads installed.

```bash
# Build WPF app
 dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj

# Run WPF tests
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj
```

## Full-solution build/test

```bash
# Build all projects
 dotnet build StegoForge.sln

# Run complete test suite
 dotnet test StegoForge.sln
```

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

- Trigger: push tags matching `v*` (for example, `v1.2.0`).
- `package-cli` job (Ubuntu) publishes CLI output from `StegoForge.Cli`.
- `package-wpf` job (Windows) publishes WPF output from `StegoForge.Wpf`.
- Release artifact names include the tag name:
  - `stegoforge-cli-<tag>`
  - `stegoforge-wpf-<tag>`

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
