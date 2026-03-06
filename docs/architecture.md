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

## Extensibility points

- Add a new format by implementing `ICarrierFormatHandler`.
- Add new crypto algorithm suite by implementing `ICryptoProvider`.
- Add new compression strategy by implementing `ICompressionProvider`.
- Register implementations through infrastructure composition root.

## Non-goals (near term)

- Cloud KMS dependency in v1 baseline.
- Lossy format support that cannot guarantee deterministic recovery.
- Hidden runtime plugin loading (explicit registration preferred).
