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
3. Application resolves compression provider (if requested) and runs compression first.
4. Application resolves crypto provider + KDF settings and encrypts the (possibly compressed) payload.
5. Payload envelope is built from finalized bytes and descriptors (metadata + payload + AEAD tag/integrity data).
6. Handler maps bits/bytes into carrier medium.
7. Result reports output path, capacity used, and diagnostics.

### Extract

1. Entry point identifies carrier and extraction parameters.
2. Handler retrieves encoded envelope bytes.
3. Envelope parser validates framing/header fields and surfaces crypto/compression descriptors.
4. If encrypted, application resolves crypto provider from header metadata and performs authenticated decryption before any decompression.
5. If compressed, application resolves compression provider from header metadata and decompresses the decrypted bytes.
6. Envelope verification and payload reconstruction.
7. Result includes payload stream/file and metadata.

## Finalized processing order and provider boundaries

Milestone 5 locks the pipeline boundary between compression and encryption:

- **Embed order is fixed:** `payload -> compression (optional) -> encryption (optional) -> envelope serialize -> carrier embed`.
- **Extract order is fixed inverse:** `carrier extract -> envelope parse -> decryption (optional) -> decompression (optional) -> payload output`.
- Compression providers only receive plaintext payload bytes and never handle password/KDF material.
- Crypto providers receive only post-compression payload bytes plus authenticated-header context and return ciphertext + auth tag metadata.
- Application orchestration (`StegoForge.Application`) is the only layer that coordinates both provider types; carrier handlers must stay crypto/compression agnostic and treat envelope bytes as opaque.

This separation keeps compression behavior deterministic, avoids leaking plaintext metadata into carrier handlers, and ensures authenticated decryption occurs before any decompression is attempted.

## PNG handler v1 scope and unsupported-mode behavior

`PngLsbFormatHandler` (`src/StegoForge.Formats/Png/PngLsbFormatHandler.cs`) defines the current PNG production scope for `png-lsb-v1`:

- supported carriers are PNG files detected by signature/format parser,
- supported pixel modes are strictly `PngColorType.Rgb` and `PngColorType.RgbWithAlpha`,
- supported bit depth is strictly 8-bit PNG,
- only RGB channels are used for payload bits (alpha is preserved but not used for embedding).

Unsupported PNG modes are intentionally fail-fast and deterministic:

- `Supports(...)` returns `false` for unsupported/non-PNG inputs,
- `GetCapacityAsync`, `EmbedAsync`, `ExtractAsync`, and `GetInfoAsync` call shared validation and throw `UnsupportedFormatException` when input violates PNG v1 policy,
- deterministic policy checks are locked by unit tests (`Supports_ReturnsFalse_ForUnsupportedGrayscaleColorType`, `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedColorType`).

## Provider selection strategy and fallback behavior

Application orchestration should resolve providers through deterministic selection rules so behavior is predictable across CLI and GUI:

1. Resolve requested algorithm id from operation options/metadata (for example, compression descriptor in payload header on extract).
2. Match by exact provider `AlgorithmId` (case-insensitive comparison recommended for UX, while diagnostics should report canonical provider id).
3. If exactly one provider matches, use it and record `AlgorithmIdentifier`/`ProviderIdentifier` in diagnostics.

Fallback behavior when a requested algorithm is unavailable:

- **Embed, explicit user-requested algorithm:** fail fast with `UnsupportedFormat` (or `InvalidArguments` when option syntax itself is invalid). Do not silently switch to another algorithm because this would change output determinism and user intent.
- **Embed, automatic/default selection:** if preferred provider is unavailable but another provider satisfies policy constraints, orchestration may fall back to the configured default provider and emit a diagnostics warning describing the fallback. If no provider is available, fail with `UnsupportedFormat`.
- **Extract, algorithm indicated by envelope metadata/flags:** never substitute a different provider. Missing provider support must fail deterministically with `UnsupportedFormat`; mismatched/corrupt compressed/encrypted bytes should continue to map to typed payload/crypto failures (`InvalidPayloadException`, `WrongPassword`, etc.) as applicable.

These rules preserve compatibility and make failure modes explicit when environments differ in installed provider sets.

## Finalized core contracts

### Compression provider contract (`ICompressionProvider`)

| Contract element | Semantics |
| --- | --- |
| Provider identity | `AlgorithmId` must be stable and machine-readable so envelope metadata and diagnostics can reference the selected codec deterministically. |
| Capability metadata | `MinimumCompressionLevel` and `MaximumCompressionLevel` declare the provider-supported inclusive level range. Callers should validate selected levels against this range before invoking compression. |
| Compression API | `Compress(CompressionRequest)` consumes immutable request data (`Data`, `CompressionLevel`, optional `DiagnosticsContext`) and returns `CompressionResponse` containing compressed bytes and the applied level. |
| Decompression API | `Decompress(DecompressionRequest)` consumes compressed bytes plus optional diagnostics context and returns `DecompressionResponse` containing decompressed bytes. |
| Null/empty handling | Request/response value objects reject null payloads and reject empty payloads to keep error behavior deterministic at the boundary. |
| Decompression failure contract | Malformed compressed data, truncation, and codec-mismatch conditions must surface as `InvalidPayloadException`. Unexpected implementation/runtime failures must be wrapped as `InternalProcessingException` (preserving the original exception as `InnerException`). |

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
