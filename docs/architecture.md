# Architecture

## Goals

StegoForge aims to keep steganography logic modular, testable, and provider-driven so that:

- Carrier format support can expand without rewriting application flow.
- Crypto/compression concerns remain replaceable and independently testable.
- CLI and GUI can share the same orchestration and core behavior.

## Solution layout

- `src/StegoForge.Core`
  - Domain contracts (`IEmbedService`, `IExtractService`, `ICapacityService`, `IInfoService`)
  - Provider abstractions (`ICarrierFormatHandler`, `ICryptoProvider`, `ICompressionProvider`)
  - Request/result models and standardized errors.
- `src/StegoForge.Application`
  - Use-case orchestration and coordination logic (planned).
- `src/StegoForge.Formats`
  - Carrier format handlers (planned).
- `src/StegoForge.Crypto`
  - Encryption, key derivation, and integrity primitives (planned).
- `src/StegoForge.Compression`
  - Compression pipeline providers (planned).
- `src/StegoForge.Infrastructure`
  - Cross-cutting wiring and hosting concerns (planned).
- `src/StegoForge.Cli`
  - Command-line entry point.
- `src/StegoForge.Wpf`
  - Desktop UI entry point.

## Layering and dependency direction

Dependencies should flow inward:

1. UI layers depend on Application/Core abstractions.
2. Application depends on Core only.
3. Concrete providers depend on Core abstractions.
4. Core does not depend on UI/Application/provider assemblies.

## Runtime flow (target)

### Embed

1. Entry point parses options and validates base arguments.
2. Application service selects carrier handler by format signature/extension.
3. Payload envelope is built (metadata + payload + integrity tag).
4. Optional compression, then optional encryption.
5. Handler maps bits/bytes into carrier medium.
6. Result reports output path, capacity used, and diagnostics.

### Extract

1. Entry point identifies carrier and extraction parameters.
2. Handler retrieves encoded envelope bytes.
3. Optional decryption, then optional decompression.
4. Envelope verification and payload reconstruction.
5. Result includes payload stream/file and metadata.

## Finalized core contracts

### Shared option/value blocks

| Type | Fields | Behavioral guarantees |
| --- | --- | --- |
| `ProcessingOptions` | `CompressionMode`, `CompressionLevel`, `EncryptionMode`, `OverwriteBehavior`, `VerbosityMode` | Defaults are stable (`Automatic`, level `5`, `Optional`, `Disallow`, `Normal`). Compression level is validated to `0-9`. |
| `PasswordOptions` | `Requirement`, `SourceHint`, `SourceReference` | Never stores a secret. Models requirement semantics and an optional secure-source hint/reference only. Whitespace source references are rejected. |
| `OperationDiagnostics` | `Warnings`, `Notes`, `Duration`, `AlgorithmIdentifier`, `ProviderIdentifier` | `Duration` cannot be negative. `Empty` provides a stable zero-value diagnostics object. |

### Request contracts

| Request | Fields | Behavioral guarantees |
| --- | --- | --- |
| `EmbedRequest` | `CarrierPath`, `OutputPath`, `Payload`, `ProcessingOptions`, `PasswordOptions` | Required paths and non-empty payload are enforced. Option blocks default to shared defaults when omitted. |
| `ExtractRequest` | `CarrierPath`, `OutputPath`, `ProcessingOptions`, `PasswordOptions` | Required paths are enforced. Option blocks default to shared defaults when omitted. |
| `CapacityRequest` | `CarrierPath`, `PayloadSizeBytes`, `ProcessingOptions` | Required carrier path and non-negative payload size are enforced. Processing options default when omitted. |
| `InfoRequest` | `CarrierPath`, `ProcessingOptions` | Required carrier path is enforced. Processing options default when omitted. |

### Response contracts

| Response | Fields | Behavioral guarantees |
| --- | --- | --- |
| `EmbedResponse` | `OutputPath`, `CarrierFormatId`, `PayloadSizeBytes`, `BytesEmbedded`, `Diagnostics` | Stable machine-readable format identifier and payload/embedded byte counts. Diagnostics always present. |
| `ExtractResponse` | `OutputPath`, `ResolvedOutputPath`, `CarrierFormatId`, `Payload`, `PayloadSizeBytes`, `OriginalFileName`, `PreservedOriginalFileName`, `IntegrityVerificationResult`, `Warnings`, `WasCompressed`, `WasEncrypted`, `Diagnostics` | Payload bytes are cloned for immutability, length is reflected into `PayloadSizeBytes`, and extraction metadata/integrity outcomes are always explicit. Diagnostics always present. |
| `CapacityResponse` | `CarrierFormatId`, `RequestedPayloadSizeBytes`, `AvailableCapacityBytes`, `MaximumCapacityBytes`, `SafeUsableCapacityBytes`, `EstimatedOverheadBytes`, `CanEmbed`, `RemainingBytes`, `FailureReason`, `ConstraintBreakdown`, `Diagnostics` | Capacity analysis distinguishes theoretical max vs safe usable capacity and reports overhead plus failure constraints when embed is not possible. Diagnostics always present. |
| `CarrierInfoResponse` | `FormatId`, `FormatDetails`, `CarrierSizeBytes`, `EstimatedCapacityBytes`, `AvailableCapacityBytes`, `EmbeddedDataPresent`, `SupportsEncryption`, `SupportsCompression`, `PayloadMetadata`, `ProtectionDescriptors`, `Diagnostics` | Provides carrier format detail, size/capacity telemetry, embedded-data signal, payload metadata summary (when readable), and compression/encryption/integrity descriptors. Diagnostics always present. |

## Error model

Errors should be expressed via `StegoErrorCode` + message + optional context so CLI/WPF can provide consistent user-facing diagnostics and machine-readable failures.

### Error taxonomy

| `StegoErrorCode` | Meaning | Expected caller behavior (CLI/GUI) |
| --- | --- | --- |
| `FileNotFound` | Required input file does not exist or is inaccessible at the provided path. | CLI should emit a concise path-focused message to stderr and return a deterministic input-related non-zero exit code. GUI should highlight the relevant path field and prompt for a corrected file selection. |
| `InvalidArguments` | Caller supplied invalid, missing, or contradictory arguments/options. | CLI should show validation guidance and usage help. GUI should keep the operation blocked until form validation errors are corrected. |
| `CorruptedData` | Carrier or embedded envelope is malformed, truncated, or fails structural/integrity validation. | CLI should report recovery failure without exposing raw internals. GUI should present a non-recoverable data warning and suggest trying a different source or backup. |
| `UnsupportedFormat` | Carrier format is not recognized or not currently supported by available handlers. | CLI should list supported formats when possible. GUI should show supported format hints near file-picker constraints. |
| `InvalidPayload` | Payload data is empty, malformed, or otherwise fails payload-level validation. | CLI should report payload validation failure and stop before processing. GUI should prompt for a valid payload source/selection. |
| `InvalidHeader` | Embedded header/metadata is invalid for the expected StegoForge envelope schema/version. | CLI should report invalid header and fail extraction deterministically. GUI should show that embedded metadata cannot be parsed for this file. |
| `WrongPassword` | Decryption failed due to an incorrect password/key or mismatched key source. | CLI should request retry with correct secret input without echoing sensitive data. GUI should allow re-entry via secure input controls. |
| `InsufficientCapacity` | Carrier cannot safely hold the requested payload with selected processing options. | CLI should surface required vs available capacity and suggest `capacity` command usage. GUI should display capacity telemetry and suggest reducing payload or changing carrier/options. |
| `OutputAlreadyExists` | Requested output path conflicts with an existing file and overwrite is disallowed. | CLI should instruct use of overwrite/force flag or alternate output path. GUI should provide overwrite confirmation or path picker. |
| `InternalProcessingFailure` | Unexpected processing failure not attributable to actionable caller input. | CLI should emit a generic user-safe message and optionally reference verbose diagnostics. GUI should show a generic failure dialog with safe details and diagnostic correlation IDs when available. |

## Extensibility points

- Add a new format by implementing `ICarrierFormatHandler`.
- Add new crypto algorithm suite by implementing `ICryptoProvider`.
- Add new compression strategy by implementing `ICompressionProvider`.
- Register implementations through infrastructure composition root.

## Non-goals (near term)

- Cloud KMS dependency in v1 baseline.
- Lossy format support that cannot guarantee deterministic recovery.
- Hidden runtime plugin loading (explicit registration preferred).


## Payload envelope error contract

Payload envelope serialization/deserialization must throw StegoForge typed exceptions only, so `StegoErrorMapper` can produce stable machine-readable errors:

- Header contract violations (`magic`, `version`, `flags`, header schema, metadata flag mismatches) -> `InvalidHeaderException`.
- Structural/bounds failures (truncation, declared lengths beyond available data, trailing bytes, inconsistent block lengths) -> `InvalidPayloadException`.
- Unexpected internal failures should be translated to `InternalProcessingException` at orchestration boundaries when needed, preserving user-safe messages.

This contract ensures deterministic error-code behavior across CLI and GUI surfaces without requiring transport-specific parsing logic in presentation layers.
