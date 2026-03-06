# Roadmap

This roadmap maps milestones 1–14 with acceptance criteria.

## Milestone 1 — Solution scaffolding and baseline projects

**Goal:** Establish layered project structure, solution wiring, and baseline startup apps.

**Acceptance criteria:**

- Solution contains Core/Application/Infrastructure/Formats/Crypto/Compression/CLI/WPF projects.
- All projects compile with shared .NET SDK/version policy.
- Baseline tests exist and pass in their target environments.

## Milestone 2 — Core contract finalization

**Goal:** Stabilize domain abstractions and request/result/error models.

**Acceptance criteria:**

- `IEmbedService`, `IExtractService`, `ICapacityService`, `IInfoService` contracts finalized.
- Common model types support expected metadata and result diagnostics.
- Error code taxonomy documented and covered by unit tests.

## Milestone 3 — Payload envelope v1

**Goal:** Implement versioned payload framing with metadata and integrity primitives.

**Implementation status (tracked):**

- [x] Envelope serializer/deserializer supports magic, version, flags, header, payload via `PayloadEnvelopeSerializer` in `src/StegoForge.Application/Payload/PayloadEnvelopeSerializer.cs`, validated by `SerializeDeserialize_RoundTrip_PreservesAllEnvelopeFields` and `Deserialize_KnownFixtureEnvelope_ProducesExpectedValues` in `tests/StegoForge.Tests.Unit/PayloadEnvelopeSerializerTests.cs`.
- [x] Truncation/corruption handling returns deterministic typed errors (`InvalidHeaderException`/`InvalidPayloadException`) validated by `Deserialize_TruncationAtStructuralBoundaries_ThrowsInvalidPayload`, `Deserialize_CorruptMagic_ThrowsInvalidHeader`, `Deserialize_CorruptVersion_ThrowsInvalidHeader`, `Deserialize_CorruptHeaderLengthPrefix_ThrowsInvalidPayload`, `Deserialize_CorruptPayloadLengthPrefix_ThrowsInvalidPayload`, and `Deserialize_CorruptIntegrityLengthPrefix_ThrowsInvalidPayload` in `tests/StegoForge.Tests.Unit/PayloadEnvelopeSerializerTests.cs`.
- [x] Binary compatibility checks are enforced by fixture/golden tests `Serialize_KnownFixtureEnvelope_ProducesExpectedGoldenBytes` and `Deserialize_KnownFixtureEnvelope_ProducesExpectedValues` in `tests/StegoForge.Tests.Unit/PayloadEnvelopeSerializerTests.cs`.

## Milestone 4 — Compression provider integration

**Goal:** Add pluggable compression for payload preprocessing.

**Implementation status (tracked):**

- [x] Deflate provider implements `ICompressionProvider` with stable `AlgorithmId` and level-range metadata in `src/StegoForge.Compression/Deflate/DeflateCompressionProvider.cs`, validated by `ProviderMetadata_UsesExpectedRangeAndAlgorithmId` in `tests/StegoForge.Tests.Unit/Compression/DeflateCompressionProviderTests.cs` and interface/contract checks in `tests/StegoForge.Tests.Unit/CompressionProviderContractTests.cs`.
- [x] Embed-time compression policy supports `Disabled`, `Enabled`, and `Automatic` modes in `src/StegoForge.Application/Payload/PayloadOrchestrationService.cs`, validated by `EmbedExtract_CompressionDisabled_SkipsCompressionAndKeepsMetadataConsistent`, `EmbedExtract_CompressionEnabled_AlwaysCompressesAndExtractDecompresses`, and `EmbedExtract_CompressionAutomatic_CompressesOnlyWhenSmaller_WithConsistentMetadataFlag` in `tests/StegoForge.Tests.Integration/CompressionOrchestrationIntegrationTests.cs`.
- [x] Decompression failure paths map malformed/truncated compressed payloads to deterministic `InvalidPayloadException`/`StegoErrorCode.InvalidPayload` outcomes in `src/StegoForge.Compression/Deflate/DeflateCompressionProvider.cs`, validated by `Decompress_ThrowsInvalidPayloadException_ForMalformedCompressedBytes`, `ExtractPayload_CompressedEnvelopeWithCorruptedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode`, and `ExtractPayload_CompressedEnvelopeWithTruncatedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode`.

## Milestone 5 — Crypto provider integration

**Goal:** Add authenticated encryption and key derivation support.

**Implementation checklist (tracked):**

- [ ] AEAD provider implementation exists in `src/StegoForge.Crypto/Aead/AesGcmCryptoProvider.cs` and is wired through `ICryptoProvider` resolution in `src/StegoForge.Application/Payload/PayloadOrchestrationService.cs`.
- [ ] KDF policy/options binding exists in `src/StegoForge.Core/Models/PasswordOptions.cs` and request mapping in `src/StegoForge.Cli/Commands/EmbedCommand.cs` + `src/StegoForge.Cli/Commands/ExtractCommand.cs`.
- [ ] Envelope crypto metadata (`CipherAlgorithmId`, `KdfAlgorithmId`, salt/nonce/tag fields) is serialized/deserialized in `src/StegoForge.Application/Payload/PayloadEnvelopeSerializer.cs`.
- [ ] Wrong-password integration tests pass:
  - `ExtractPayload_EncryptedEnvelope_WithWrongPassword_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithWrongPassword_MapsToStegoErrorCodeWrongPassword`
  - `CliExtract_WithWrongPassword_ReturnsExitCode8`
- [ ] Tamper-detection tests pass:
  - `ExtractPayload_EncryptedEnvelope_WithTamperedCiphertext_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithTamperedAuthTag_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithTamperedCipherAlgorithmId_ThrowsInvalidHeaderException`
  - `CliExtract_WithTamperedCiphertext_ReturnsExitCode8`

## Milestone 6 — PNG format handler (v1)

**Goal:** Deliver first production-capable image carrier handler.

**Acceptance criteria:**

- PNG capacity estimation available through `ICapacityService`.
- Embed/extract round-trips pass for representative files.
- Output image integrity remains valid and viewable.

## Milestone 7 — BMP format handler

**Goal:** Add a simple uncompressed image carrier for baseline operations.

**Acceptance criteria:**

- BMP handler implements embed/extract/capacity contracts.
- Handler rejects unsupported pixel formats with deterministic errors.
- Regression tests cover boundary payload sizes.

## Milestone 8 — WAV format handler

**Goal:** Expand support to PCM audio carriers.

**Acceptance criteria:**

- WAV handler supports targeted PCM formats and documents limits.
- Capacity and round-trip correctness validated by integration tests.
- Non-PCM/unhandled variants are rejected safely.

## Milestone 9 — Application orchestration and policy rules

**Goal:** Implement use-case orchestration with cohesive validation rules.

**Acceptance criteria:**

- Application services coordinate provider selection and pipeline ordering.
- Validation rules catch invalid combinations before processing.
- End-to-end tests verify consistent behavior across handlers.

## Milestone 10 — CLI command surface v1

**Goal:** Ship user-facing CLI commands for embed/extract/capacity/info.

**Acceptance criteria:**

- CLI supports discoverable help and examples.
- Exit codes are deterministic and test-covered.
- Structured/log-friendly output mode exists for automation.

## Milestone 11 — WPF GUI v1

**Goal:** Deliver first usable desktop GUI workflow.

**Acceptance criteria:**

- GUI supports embed/extract flows with validation.
- Progress and error states are user-readable and consistent.
- Basic UI smoke tests pass on Windows runners.

## Milestone 12 — Hardening and robustness

**Goal:** Improve resilience, diagnostics, and secure defaults.

**Acceptance criteria:**

- Fuzz/negative-path tests added for payload parsing and handlers.
- Resource-usage safeguards (size limits/timeouts where needed) implemented.
- Logging/diagnostics provide enough context without leaking secrets.

## Milestone 13 — Documentation and developer experience

**Goal:** Complete primary docs for contributors and operators.

**Acceptance criteria:**

- README and docs set cover architecture, payload format, build, testing, CLI, GUI.
- Build and test commands validated in CI docs.
- Contribution guidance aligns with project conventions.

## Milestone 14 — Release readiness (v1.0)

**Goal:** Prepare stable release candidate and publish artifacts.

**Acceptance criteria:**

- Versioning/changelog/release notes process is documented and executed.
- All critical tests pass in CI across required platforms.
- Signed binaries/packages and verification instructions are published.
