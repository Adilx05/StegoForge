# Roadmap

_Last verified against source: 2026-03-07 (`0fd7c07`)._
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

- [x] AEAD provider implementation exists in `src/StegoForge.Crypto/AesGcm/AesGcmCryptoProvider.cs` and is wired through `ICryptoProvider` resolution in `src/StegoForge.Application/Payload/PayloadOrchestrationService.cs`.
- [x] KDF policy/options binding exists in `src/StegoForge.Core/Models/OperationOptions.cs` and request mapping in `src/StegoForge.Cli/Commands/EmbedCommand.cs` + `src/StegoForge.Cli/Commands/ExtractCommand.cs`.
- [x] Envelope crypto metadata (`CipherAlgorithmId`, `KdfAlgorithmId`, salt/nonce/tag fields) is serialized/deserialized in `src/StegoForge.Application/Payload/PayloadEnvelopeSerializer.cs`.
- [x] Wrong-password integration tests pass:
  - `ExtractPayload_EncryptedEnvelope_WithWrongPassword_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithWrongPassword_MapsToStegoErrorCodeWrongPassword`
  - `CliExtract_WithWrongPassword_ReturnsExitCode8`
- [x] Tamper-detection tests pass:
  - `ExtractPayload_EncryptedEnvelope_WithTamperedCiphertext_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithTamperedAuthTag_ThrowsWrongPasswordException`
  - `ExtractPayload_EncryptedEnvelope_WithTamperedCipherAlgorithmId_ThrowsInvalidHeaderException`
  - `CliExtract_WithTamperedCiphertext_ReturnsExitCode8`

## Milestone 6 — PNG format handler (v1)

**Goal:** Deliver first production-capable image carrier handler.

**Implementation checklist (tracked):**

- [x] `PngLsbFormatHandler` is the production PNG v1 carrier and remains wired for embed/extract/capacity/info in `src/StegoForge.Formats/Png/PngLsbFormatHandler.cs`.
- [x] PNG capacity estimations are validated through integration entry points:
  - `GetCapacityAsync_PngCarrier_ReturnsExpectedFormatAndCanEmbedDecisions` in `tests/StegoForge.Tests.Integration/CapacityServiceIntegrationTests.cs`
  - deterministic calculator boundaries in `tests/StegoForge.Tests.Unit/Png/PngLsbCapacityCalculatorTests.cs`
- [x] PNG round-trip coverage passes for representative processing modes in `tests/StegoForge.Tests.Integration/PngRoundTripIntegrationTests.cs`:
  - `EmbedExtract_BasicRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_CompressedAndEncryptedRoundTrip_ProducesByteIdenticalPayload`
- [x] PNG output validity checks remain enforced in `tests/StegoForge.Tests.Integration/PngRoundTripIntegrationTests.cs` via IHDR parsing + `Image.Identify` + `Image.LoadAsync` path (`ValidateEmbeddedPngAsync`).
- [x] Unsupported PNG modes are rejected deterministically by tests:
  - `Supports_ReturnsFalse_ForUnsupportedGrayscaleColorType`
  - `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedColorType`

## Milestone 7 — BMP format handler

**Goal:** Add a simple uncompressed image carrier for baseline operations.

**Implementation checklist (tracked):**

- [x] `BmpLsbFormatHandler` remains the production BMP v1 carrier and is wired for embed/extract/capacity/info flows in `src/StegoForge.Formats/Bmp/BmpLsbFormatHandler.cs`.
- [x] BMP capacity boundaries are covered by deterministic tests:
  - `Calculate_TinyImages_ReportZeroAndNearZeroSafeUsableCapacity`
  - `Calculate_ExactFitPayload_ReturnsEmbeddableWithoutDiagnostics`
  - `Calculate_OverCapacityByOneByte_ReturnsDeterministicOverflowDiagnostic`
  - `GetCapacityAsync_BmpCarrier_ResolvesBmpHandler`
  - files: `tests/StegoForge.Tests.Unit/Bmp/BmpLsbCapacityCalculatorTests.cs`, `tests/StegoForge.Tests.Integration/BmpCapacityServiceIntegrationTests.cs`.
- [x] BMP round-trip integration coverage passes in `tests/StegoForge.Tests.Integration/BmpRoundTripIntegrationTests.cs`:
  - `EmbedExtract_SmallBmpFixture_ProducesByteIdenticalRoundTrip`
  - `EmbedExtract_MediumBmpFixture_ProducesByteIdenticalRoundTrip`
  - `Extract_CorruptedCarrier_MapsToDeterministicCorruptedDataErrorCode`
- [x] Unsupported BMP variants are rejected with deterministic errors in `tests/StegoForge.Tests.Unit/Bmp/BmpLsbFormatHandlerTests.cs`:
  - `Supports_ReturnsFalse_ForUnsupportedBitDepth`
  - `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedBitDepth`
  - `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedCompressionMode`
  - `EmbedAsync_ThrowsInvalidHeader_ForTruncatedBmpHeader`

## Milestone 8 — WAV format handler

**Goal:** Expand support to PCM audio carriers.

**Implementation checklist (tracked):**

- [x] `WavLsbFormatHandler` remains the production WAV v1 carrier and is wired for embed/extract/capacity/info flows in `src/StegoForge.Formats/Wav/WavLsbFormatHandler.cs`.
- [x] WAV capacity boundaries are covered by deterministic tests:
  - `CalculateFromSampleCount_TinyCarrier_ReturnsZeroSafeCapacityWithDeterministicDiagnostics`
  - `CalculateFromSampleCount_ExactFitPayload_CanEmbedWithoutDiagnostics`
  - `CalculateFromSampleCount_OverflowByOneByte_ReturnsDeterministicDiagnostics`
  - `GetCapacityAsync_WavCarrier_ReturnsExpectedDeterministicCapacityAndOverCapacityDiagnostics`
  - files: `tests/StegoForge.Tests.Unit/Wav/WavLsbCapacityCalculatorTests.cs`, `tests/StegoForge.Tests.Integration/WavCapacityServiceIntegrationTests.cs`.
- [x] WAV round-trip integration coverage passes in `tests/StegoForge.Tests.Integration/WavRoundTripIntegrationTests.cs`:
  - `EmbedExtract_BaselineRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload`
  - `EmbedExtract_EncryptedAndCompressedRoundTrip_ProducesByteIdenticalPayload`
- [x] Unsupported-format and malformed-carrier WAV variants are rejected with deterministic errors:
  - `EmbedAsync_WithNonPcmFormatTag_ThrowsUnsupportedFormat`
  - `ExtractAsync_WithUnsupportedBitDepth_ThrowsUnsupportedFormat`
  - `GetCapacityAsync_WithTruncatedHeader_ThrowsInvalidHeader`
  - `GetCapacityAsync_WithMissingRequiredChunks_ThrowsInvalidHeader`
  - `GetCapacityAsync_WithMissingDataChunk_ThrowsInvalidHeader`
  - file: `tests/StegoForge.Tests.Unit/Wav/WavLsbFormatValidationTests.cs`.

## Milestone 9 — Application orchestration and policy rules

**Goal:** Implement use-case orchestration with cohesive validation rules.

**Implementation checklist (tracked):**

- [x] Shared application orchestration services remain the single coordination path for embed/extract/capacity/info requests:
  - `src/StegoForge.Application/Embed/EmbedService.cs`
  - `src/StegoForge.Application/Extract/ExtractService.cs`
  - `src/StegoForge.Application/Capacity/CapacityService.cs`
  - `src/StegoForge.Application/Info/InfoService.cs`
  - `src/StegoForge.Application/Payload/PayloadOrchestrationService.cs`
- [x] Service registration keeps orchestration dependencies consistent via `src/StegoForge.Application/ApplicationServiceCollectionExtensions.cs`, validated by `AddStegoForgeApplicationServices_ResolvesAllApplicationServices` in `tests/StegoForge.Tests.Integration/ApplicationServiceOrchestrationIntegrationTests.cs`.
- [x] Policy-gate rules are enforced before handler I/O via `src/StegoForge.Application/Validation/OperationPolicyValidator.cs`, validated by:
  - `ValidateEmbedRequest_ThrowsInvalidArguments_WhenEncryptionRequiredAndPasswordSourceMissing`
  - `ValidateEmbedRequest_ThrowsInvalidArguments_WhenCompressionModeDisabledButCompressionLevelNotZero`
  - `Embed_InvalidPolicyCombination_FailsBeforeHandlerIo`
  - `Extract_InvalidPolicyCombination_FailsBeforeHandlerIo`
  - files: `tests/StegoForge.Tests.Unit/Application/OperationPolicyValidatorTests.cs`, `tests/StegoForge.Tests.Integration/OrchestrationConsistencyIntegrationTests.cs`.
- [x] Format resolution and precedence rules stay deterministic via `src/StegoForge.Application/Formats/CarrierFormatResolver.cs`, validated by:
  - `Resolve_ReturnsSingleMatchingHandler`
  - `Resolve_UsesDeterministicPrecedence_WhenMultipleHandlersMatch`
  - `Resolve_ThrowsUnsupportedFormat_WhenNoHandlerMatches`
  - file: `tests/StegoForge.Tests.Unit/Application/CarrierFormatResolverTests.cs`.
- [x] Cross-format orchestration consistency is covered by integration tests in `tests/StegoForge.Tests.Integration/OrchestrationConsistencyIntegrationTests.cs`:
  - `EmbedExtract_RoundTripConsistency_IsEquivalentAcrossFormats`
  - `Embed_WhenCapacityIsInsufficient_UsesSameErrorSemanticsPatternAcrossFormats`
  - `GetInfo_ResponseContract_IsComparableAcrossFormats`
  - `Extract_EncryptedPayload_WrongPasswordAndTamperPaths_AreCoveredViaApplicationServices`.

## Milestone 10 — CLI command surface v1

**Goal:** Ship user-facing CLI commands for embed/extract/capacity/info.

**Implementation checklist (tracked):**

- [x] CLI help and command discovery remain stable through `src/StegoForge.Cli/CliApplication.cs` and command builders in `src/StegoForge.Cli/Commands/`:
  - `HelpFlag_IsDiscoverable_AndPrintsCommandCatalog`
  - `HelpCommand_IsDiscoverable`
  - `Version_Command_IsDiscoverable`
  - file: `tests/StegoForge.Tests.Cli/CliCommandIntegrationTests.cs`.
- [x] Command pipeline exit-code determinism is locked by tests exercising success and mapped failures in `src/StegoForge.Cli/Commands/CommandExecution.cs` and `src/StegoForge.Cli/CliErrorContract.cs`:
  - `EmbedCommand_ReturnsMappedFailureCodeAndStableStderrFormat`
  - `ExtractCommand_ReturnsMappedFailureCode`
  - `CapacityCommand_ReturnsMappedFailureCode`
  - `InfoCommand_ReturnsMappedFailureCode`
  - parser failure mappings:
    - `ParserError_MissingRequiredOptions_ReturnsInvalidArgumentsCodeAndStableMessageShape`
    - `ParserError_InvalidEnumValue_ReturnsInvalidArgumentsCodeAndStableMessageShape`
    - `ParserError_InvalidFileArgument_ReturnsInvalidArgumentsCodeAndStableMessageShape`
  - file: `tests/StegoForge.Tests.Cli/CommandPipelineExitCodeTests.cs`.
- [x] JSON output contract behavior is stable for both success and failure paths via `src/StegoForge.Cli/Output/` and error serialization in `src/StegoForge.Cli/Commands/CommandExecution.cs`:
  - `InfoCommand_JsonSuccess_EmitsStableContractFields`
  - `CapacityCommand_JsonFailure_EmitsStableErrorShapeAndExitCode`
  - `ParserError_WithJsonFlag_EmitsJsonErrorShape`
  - `Info_ReportsMetadataPresenceAndAbsence_AsJson`
  - file: `tests/StegoForge.Tests.Cli/CommandPipelineExitCodeTests.cs`, `tests/StegoForge.Tests.Cli/CliCommandIntegrationTests.cs`.

## Milestone 11 — WPF GUI v1

**Goal:** Deliver first usable desktop GUI workflow.

**Implementation checklist (tracked):**

- [x] Embed/extract views are implemented with concrete field wiring and commands in:
  - `src/StegoForge.Wpf/Views/EmbedView.xaml`
  - `src/StegoForge.Wpf/Views/ExtractView.xaml`
  - `src/StegoForge.Wpf/ViewModels/EmbedViewModel.cs`
  - `src/StegoForge.Wpf/ViewModels/ExtractViewModel.cs`
- [x] UI validation behavior is covered by WPF-focused tests:
  - `EmbedViewModel_ReportsValidationMessage_ForMissingRequiredFields`
  - `EmbedViewModel_CommandCanExecute_TransitionsWithValidationState`
  - `ExtractViewModel_CommandCanExecute_TransitionsWithValidationState`
  - file: `tests/StegoForge.Tests.Wpf/ViewModelValidationTests.cs`
- [x] Progress/error operation state semantics are exercised by command-flow/state tests:
  - `EmbedCommand_TracksBusyLifecycle_AndCompletionState`
  - `ExtractCommand_MapsErrors_UsingSharedErrorMapper`
  - `ExtractCommand_ResetsStatusBetweenOperations`
  - files: `tests/StegoForge.Tests.Wpf/ViewModelOperationStateTests.cs`, `tests/StegoForge.Tests.Wpf/WpfCommandFlowTests.cs`
- [x] Windows-capable smoke coverage verifies composition/startup for the v1 GUI shell:
  - `CompositionContainer_ResolvesMainWindowAndOperationViewModels`
  - `CommandBindings_InitializeWithoutExceptions`
  - `StartupCompositionPath_ResolvesMainWindow`
  - file: `tests/StegoForge.Tests.Wpf/WpfCompositionSmokeTests.cs`

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
