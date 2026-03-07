# Building

_Last verified against source: 2026-03-07 (`0fd7c07`)._
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

## CLI-focused build/test

```bash
# Build CLI app and CLI test project only
 dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj
 dotnet build tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

# Run all CLI tests
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

# Targeted command-surface suites: help discoverability + version
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~HelpFlag_IsDiscoverable_AndPrintsCommandCatalog|FullyQualifiedName~HelpCommand_IsDiscoverable|FullyQualifiedName~Version_Command_IsDiscoverable"

# Targeted command-surface suites: exit-code determinism
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~EmbedCommand_ReturnsMappedFailureCodeAndStableStderrFormat|FullyQualifiedName~ExtractCommand_ReturnsMappedFailureCode|FullyQualifiedName~CapacityCommand_ReturnsMappedFailureCode|FullyQualifiedName~InfoCommand_ReturnsMappedFailureCode|FullyQualifiedName~ParserError_MissingRequiredOptions_ReturnsInvalidArgumentsCodeAndStableMessageShape|FullyQualifiedName~ParserError_InvalidEnumValue_ReturnsInvalidArgumentsCodeAndStableMessageShape|FullyQualifiedName~ParserError_InvalidFileArgument_ReturnsInvalidArgumentsCodeAndStableMessageShape"

# Targeted command-surface suites: JSON success/failure contracts
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~InfoCommand_JsonSuccess_EmitsStableContractFields|FullyQualifiedName~CapacityCommand_JsonFailure_EmitsStableErrorShapeAndExitCode|FullyQualifiedName~ParserError_WithJsonFlag_EmitsJsonErrorShape|FullyQualifiedName~Info_ReportsMetadataPresenceAndAbsence_AsJson"
```


## WPF-only build/test (Windows)

> Run these on Windows with desktop workloads installed.

```powershell
# Restore only the GUI app + GUI test project graph
 dotnet restore src/StegoForge.Wpf/StegoForge.Wpf.csproj
 dotnet restore tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj

# Build WPF app only
 dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj -c Release

# Build WPF test project only
 dotnet build tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj -c Release

# Run complete WPF-focused test suite
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj -c Release

# Optional focused suites: validation + operation-state + composition smoke
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj -c Release --filter "FullyQualifiedName~ViewModelValidationTests|FullyQualifiedName~ViewModelOperationStateTests|FullyQualifiedName~WpfCompositionSmokeTests"
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
