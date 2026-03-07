# Testing Strategy

## Test project map

- `tests/StegoForge.Tests.Unit`
  - Fast tests for domain models, interfaces, error mapping, and provider behavior with stubs/mocks.
- `tests/StegoForge.Tests.Integration`
  - Cross-assembly tests for real embed/extract flows and service composition.
- `tests/StegoForge.Tests.Cli`
  - CLI command contract tests and exit-code behavior.
- `tests/StegoForge.Tests.Wpf`
  - WPF-level smoke tests where environment permits.

## Quality gates (target)

- All test projects pass on CI.
- Build warnings are reviewed and kept low/noise-free.
- New handlers/providers include both positive and negative-path coverage.
- Regression tests accompany fixed defects.

## Recommended local commands

```bash
# Full test pass
 dotnet test StegoForge.sln

# Focused runs
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj
 dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj
```


## WPF smoke test scope

`tests/StegoForge.Tests.Wpf` is intentionally lightweight and headless-runner friendly for `windows-latest` CI agents:

- DI/composition smoke coverage verifies service registration and construction for `MainWindowViewModel`, `EmbedViewModel`, and `ExtractViewModel`.
- Startup composition coverage verifies the app startup path can resolve `MainWindow` with a valid `MainWindowViewModel` data context.
- Command binding smoke coverage verifies command properties (`CanExecute`) initialize without throwing exceptions.
- UI-thread-aware command-flow tests use `[WpfFact]` plus mocked/stubbed dependencies to validate embed/extract invocation state transitions without brittle pixel/UI automation.
- Assembly-level test parallelization is disabled to avoid dispatcher-thread contention and improve reliability on headless Windows runners.

Recommended focused command:

```bash
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj
```

## CLI contract test matrix

Use the CLI test project as the acceptance source for command-surface behavior:

- Project: `tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj`
- Run everything: `dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj`

### Help discoverability

- `HelpFlag_IsDiscoverable_AndPrintsCommandCatalog`
- `HelpCommand_IsDiscoverable`
- `Version_Command_IsDiscoverable`

Filter command:

```bash
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~HelpFlag_IsDiscoverable_AndPrintsCommandCatalog|FullyQualifiedName~HelpCommand_IsDiscoverable|FullyQualifiedName~Version_Command_IsDiscoverable"
```

### Exit-code determinism

- `EmbedCommand_ReturnsMappedFailureCodeAndStableStderrFormat`
- `ExtractCommand_ReturnsMappedFailureCode`
- `CapacityCommand_ReturnsMappedFailureCode`
- `InfoCommand_ReturnsMappedFailureCode`
- `ParserError_MissingRequiredOptions_ReturnsInvalidArgumentsCodeAndStableMessageShape`
- `ParserError_InvalidEnumValue_ReturnsInvalidArgumentsCodeAndStableMessageShape`
- `ParserError_InvalidFileArgument_ReturnsInvalidArgumentsCodeAndStableMessageShape`

Filter command:

```bash
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~EmbedCommand_ReturnsMappedFailureCodeAndStableStderrFormat|FullyQualifiedName~ExtractCommand_ReturnsMappedFailureCode|FullyQualifiedName~CapacityCommand_ReturnsMappedFailureCode|FullyQualifiedName~InfoCommand_ReturnsMappedFailureCode|FullyQualifiedName~ParserError_MissingRequiredOptions_ReturnsInvalidArgumentsCodeAndStableMessageShape|FullyQualifiedName~ParserError_InvalidEnumValue_ReturnsInvalidArgumentsCodeAndStableMessageShape|FullyQualifiedName~ParserError_InvalidFileArgument_ReturnsInvalidArgumentsCodeAndStableMessageShape"
```

### JSON output behavior

- `InfoCommand_JsonSuccess_EmitsStableContractFields`
- `CapacityCommand_JsonFailure_EmitsStableErrorShapeAndExitCode`
- `ParserError_WithJsonFlag_EmitsJsonErrorShape`
- `Info_ReportsMetadataPresenceAndAbsence_AsJson`

Filter command:

```bash
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --filter "FullyQualifiedName~InfoCommand_JsonSuccess_EmitsStableContractFields|FullyQualifiedName~CapacityCommand_JsonFailure_EmitsStableErrorShapeAndExitCode|FullyQualifiedName~ParserError_WithJsonFlag_EmitsJsonErrorShape|FullyQualifiedName~Info_ReportsMetadataPresenceAndAbsence_AsJson"
```

## Payload Envelope v1

Acceptance criteria traceability for `PayloadEnvelopeSerializerTests`:

- **Envelope serializer/deserializer supports magic, version, flags, header, payload.**
  - `SerializeDeserialize_RoundTrip_PreservesAllEnvelopeFields`
  - `Deserialize_KnownFixtureEnvelope_ProducesExpectedValues`
- **Truncation/corruption handling returns deterministic errors.**
  - `Deserialize_TruncationAtStructuralBoundaries_ThrowsInvalidPayload`
  - `Deserialize_CorruptMagic_ThrowsInvalidHeader`
  - `Deserialize_CorruptVersion_ThrowsInvalidHeader`
  - `Deserialize_CorruptHeaderLengthPrefix_ThrowsInvalidPayload`
  - `Deserialize_CorruptPayloadLengthPrefix_ThrowsInvalidPayload`
  - `Deserialize_CorruptIntegrityLengthPrefix_ThrowsInvalidPayload`
  - `Deserialize_ReservedUnknownFlagBits_AreRejectedByV1Policy`
  - `Serialize_ReservedUnknownFlagBits_AreRejectedByV1Policy`
- **Round-trip tests validate binary compatibility expectations.**
  - `Serialize_KnownFixtureEnvelope_ProducesExpectedGoldenBytes`
  - `Deserialize_KnownFixtureEnvelope_ProducesExpectedValues`


## Compression failure-path coverage

The following tests lock down extraction behavior when compressed envelope payload bytes are deliberately corrupted:

- `ExtractPayload_CompressedEnvelopeWithCorruptedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode`
  - Expected typed exception: `InvalidPayloadException`
  - Expected mapped error code: `StegoErrorCode.InvalidPayload`
- `ExtractPayload_CompressedEnvelopeWithTruncatedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode`
  - Expected typed exception: `InvalidPayloadException`
  - Expected mapped error code: `StegoErrorCode.InvalidPayload`
- `CreateFailureFromException_DecompressionCorruption_MapsToInvalidPayloadDeterministically`
  - Expected mapped error code: `StegoErrorCode.InvalidPayload`
  - Expected CLI exit code: `6`
- `CreateFailureFromException_DecompressionUnexpectedFailure_ProducesNonZeroExitCode`
  - Expected mapped error code: `StegoErrorCode.InternalProcessingFailure`
  - Expected CLI exit code: `1`


## BMP v1

### Boundary coverage

- `Calculate_TinyImages_ReportZeroAndNearZeroSafeUsableCapacity`
- `Calculate_ExactFitPayload_ReturnsEmbeddableWithoutDiagnostics`
- `Calculate_OverCapacityByOneByte_ReturnsDeterministicOverflowDiagnostic`
- `EmbedAndExtract_RoundTripsExactFitPayload`
- `EmbedAsync_WhenPayloadExceedsCapacityByOneByte_ThrowsInsufficientCapacity`

### Unsupported-format coverage

- `Supports_ReturnsFalse_ForUnsupportedBitDepth`
- `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedBitDepth`
- `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedCompressionMode`
- `EmbedAsync_ThrowsInvalidHeader_ForTruncatedBmpHeader`


## WAV v1

Acceptance criteria traceability for WAV carrier support (`wav-lsb-v1`) and deterministic failure behavior.

### Supported carrier implementation (`WavLsbFormatHandler`)

- Implementation file: `src/StegoForge.Formats/Wav/WavLsbFormatHandler.cs`
- v1 supported set: RIFF/WAVE, `fmt` format tag `1` (PCM), 16-bit little-endian samples, mono/stereo channels, aligned `data` chunk.

### Capacity acceptance coverage

- `CalculateFromSampleCount_TinyCarrier_ReturnsZeroSafeCapacityWithDeterministicDiagnostics`
  - Covers tiny-carrier floor behavior (`0` safe bytes) plus deterministic diagnostics text.
- `CalculateFromSampleCount_ExactFitPayload_CanEmbedWithoutDiagnostics`
  - Covers exact-fit payload acceptance with no constraint diagnostics.
- `CalculateFromSampleCount_OverflowByOneByte_ReturnsDeterministicDiagnostics`
  - Covers boundary overflow (+1 byte) deterministic rejection and diagnostic detail.
- `GetCapacityAsync_WavCarrier_ReturnsExpectedDeterministicCapacityAndOverCapacityDiagnostics`
  - Covers integration-level `capacity` behavior and over-capacity diagnostics through application services.

### Round-trip acceptance coverage

- `EmbedExtract_BaselineRoundTrip_ProducesByteIdenticalPayload`
  - Covers unencrypted/uncompressed embed→extract byte-identical behavior.
- `EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload`
  - Covers compression-enabled WAV round trip.
- `EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload`
  - Covers encryption-enabled WAV round trip.
- `EmbedExtract_EncryptedAndCompressedRoundTrip_ProducesByteIdenticalPayload`
  - Covers combined compression+encryption WAV round trip.

### Unsupported-format and malformed-header acceptance coverage

- `EmbedAsync_WithNonPcmFormatTag_ThrowsUnsupportedFormat`
  - Covers non-PCM rejection (`UnsupportedFormat`).
- `ExtractAsync_WithUnsupportedBitDepth_ThrowsUnsupportedFormat`
  - Covers unsupported bit-depth rejection (`UnsupportedFormat`).
- `GetCapacityAsync_WithTruncatedHeader_ThrowsInvalidHeader`
  - Covers truncated RIFF/WAVE header rejection (`InvalidHeader`).
- `GetCapacityAsync_WithMissingRequiredChunks_ThrowsInvalidHeader`
  - Covers missing mandatory chunk layout rejection (`InvalidHeader`).
- `GetCapacityAsync_WithMissingDataChunk_ThrowsInvalidHeader`
  - Covers missing `data` chunk rejection (`InvalidHeader`).

## Milestone 5 — Crypto wrong-password and tamper matrix

The following test cases are required for Milestone 5 completion. Names are intentionally fixed so roadmap/docs/test reviews can reference them verbatim.

### Wrong-password behavior (must map to `WrongPassword`)

- `ExtractPayload_EncryptedEnvelope_WithWrongPassword_ThrowsWrongPasswordException`
- `ExtractPayload_EncryptedEnvelope_WithWrongPassword_MapsToStegoErrorCodeWrongPassword`
- `CliExtract_WithWrongPassword_ReturnsExitCode8`
- `EmbedThenExtract_WithPasswordFromEnv_AndMismatchedEnvValue_FailsWrongPassword`
- `EmbedThenExtract_WithPasswordFromFile_AndMismatchedFileContent_FailsWrongPassword`

### Tamper/authentication behavior (must never produce plaintext)

- `ExtractPayload_EncryptedEnvelope_WithTamperedCiphertext_ThrowsWrongPasswordException`
- `ExtractPayload_EncryptedEnvelope_WithTamperedAuthTag_ThrowsWrongPasswordException`
- `ExtractPayload_EncryptedEnvelope_WithTamperedNonceMetadata_ThrowsWrongPasswordException`
- `ExtractPayload_EncryptedEnvelope_WithTamperedSaltMetadata_ThrowsWrongPasswordException`
- `ExtractPayload_EncryptedEnvelope_WithTamperedCipherAlgorithmId_ThrowsInvalidHeaderException`
- `CliExtract_WithTamperedCiphertext_ReturnsExitCode8`

### Boundary/contract checks for crypto metadata

- `Deserialize_EncryptedEnvelope_WithMissingCipherAlgorithmId_ThrowsInvalidHeader`
- `Deserialize_EncryptedEnvelope_WithTagLengthMismatch_ThrowsInvalidPayload`
- `Deserialize_UnencryptedEnvelope_WithUnexpectedKdfMetadata_ThrowsInvalidHeader`

Each case should assert both typed exception behavior and mapped `StegoErrorCode` (plus CLI exit code where applicable) so failures remain deterministic across application, integration, and CLI layers.


## PNG embed output integrity validation

`PngRoundTripIntegrationTests` validates every embedded PNG through two independent decode/parse paths before extraction assertions run:

1. **Raw IHDR parser in tests** reads PNG signature + IHDR chunk directly from bytes to assert width/height and color-type invariants without ImageSharp metadata helpers.
2. **ImageSharp identify/load path** runs `Image.Identify` and full `Image.Load` decode to confirm the output can be fully parsed and decoded.

The integration tests assert these invariants for embedded output images:

- dimensions remain unchanged from the carrier image,
- PNG color-type policy is preserved (`Rgb` remains `Rgb`, `RgbWithAlpha` remains `RgbWithAlpha`),
- the output PNG is fully decodable with no decoder exceptions,
- extraction still succeeds from the validated output stream.

Negative-path coverage also intentionally corrupts embedded output bytes (while keeping the PNG structurally decodable) and verifies deterministic typed parser failures:

- corrupted envelope header bytes -> `InvalidHeaderException`,
- truncated envelope payload bytes -> `InvalidPayloadException`.

## PNG v1 acceptance tests

The following tests are the PNG v1 acceptance set and should be referenced by exact name in reviews, roadmap checks, and CI filters.

### Capacity and sizing acceptance

- `GetCapacityAsync_PngCarrier_ReturnsExpectedFormatAndCanEmbedDecisions`
- `Calculate_TinyImage_ReturnsZeroSafeCapacityAndDeterministicConstraintDiagnostics`
- `Calculate_ExactFitPayload_CanEmbedWithZeroRemainingHeadroom`
- `Calculate_OverCapacityByOneByte_ReturnsDeterministicOverflowDiagnostic`

### Round-trip and policy acceptance

- `EmbedExtract_BasicRoundTrip_ProducesByteIdenticalPayload`
- `EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload`
- `EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload`
- `EmbedExtract_CompressedAndEncryptedRoundTrip_ProducesByteIdenticalPayload`
- `Embed_WhenCarrierHasInsufficientCapacity_ThrowsDeterministicErrorTypeAndCode`
- `Extract_EncryptedPayload_WithWrongPassword_MapsToWrongPasswordError`

### Image validity and parser-failure acceptance

- `Extract_WhenEmbeddedEnvelopeHeaderIsCorrupted_ThrowsInvalidHeaderException`
- `Extract_WhenEmbeddedEnvelopePayloadIsTruncated_ThrowsInvalidPayloadException`
- `Supports_ReturnsFalse_ForUnsupportedGrayscaleColorType`
- `EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedColorType`


## Application orchestration and policy

The application layer uses shared orchestration/policy services so CLI and GUI behavior stays aligned. The following tests should be treated as the authoritative orchestration+policy contract set.

### Service registration and shared orchestration guarantees

- `AddStegoForgeApplicationServices_ResolvesAllApplicationServices`
  - Guarantees DI registration always resolves `IEmbedService`, `IExtractService`, `ICapacityService`, and `IInfoService` from one application-service graph.
- `EmbeddedRoundTrip_UsesApplicationServicesWithSharedOrchestration`
  - Guarantees embed/info/extract flow through shared orchestration services and reports deterministic provider identifiers.

File: `tests/StegoForge.Tests.Integration/ApplicationServiceOrchestrationIntegrationTests.cs`.

### Operation-policy guard guarantees

- `ValidateEmbedRequest_AllowsValidCombination`
  - Guarantees valid embed request combinations pass policy validation.
- `ValidateEmbedRequest_ThrowsInvalidArguments_WhenEncryptionRequiredAndPasswordSourceMissing`
  - Guarantees encryption-required requests fail fast when password source metadata is incomplete.
- `ValidateEmbedRequest_ThrowsInvalidArguments_WhenCompressionModeDisabledButCompressionLevelNotZero`
  - Guarantees invalid compression-mode/level combinations fail before processing.
- `ValidateEmbedRequest_ThrowsOutputExists_WhenOutputExistsAndOverwriteDisallowed`
  - Guarantees overwrite policy failures map deterministically to `OutputAlreadyExists`.
- `Embed_InvalidPolicyCombination_FailsBeforeHandlerIo`
- `Extract_InvalidPolicyCombination_FailsBeforeHandlerIo`
  - Guarantee invalid application-policy combinations are rejected before any carrier handler I/O.

Files: `tests/StegoForge.Tests.Unit/Application/OperationPolicyValidatorTests.cs`, `tests/StegoForge.Tests.Integration/OrchestrationConsistencyIntegrationTests.cs`.

### Resolver and cross-format consistency guarantees

- `Resolve_ReturnsSingleMatchingHandler`
- `Resolve_UsesDeterministicPrecedence_WhenMultipleHandlersMatch`
- `Resolve_ThrowsUnsupportedFormat_WhenNoHandlerMatches`
  - Guarantee deterministic handler selection/precedence and deterministic unsupported-format outcomes.
- `EmbedExtract_RoundTripConsistency_IsEquivalentAcrossFormats`
- `Embed_WhenCapacityIsInsufficient_UsesSameErrorSemanticsPatternAcrossFormats`
- `GetInfo_ResponseContract_IsComparableAcrossFormats`
- `Extract_EncryptedPayload_WrongPasswordAndTamperPaths_AreCoveredViaApplicationServices`
  - Guarantee consistent embed/extract/info behavior and deterministic error semantics across PNG/BMP/WAV handlers.

Files: `tests/StegoForge.Tests.Unit/Application/CarrierFormatResolverTests.cs`, `tests/StegoForge.Tests.Integration/OrchestrationConsistencyIntegrationTests.cs`.

## Test categories to prioritize

1. **Payload framing correctness**
   - Round-trip serialization/deserialization.
   - Invalid/truncated payload handling.
2. **Security behavior**
   - Wrong password/key material fails cleanly.
   - Tampered payload detection.
3. **Format handling**
   - Capacity calculations are deterministic.
   - Embed/extract byte accuracy for each supported format.
4. **User interface contracts**
   - CLI option parsing and clear diagnostics.
   - GUI validation and user feedback consistency.

## Performance and stress checks (planned)

- Capacity boundary tests (near-max payloads).
- Large-file memory profile checks.
- Multi-file batch throughput benchmarking.

## CI guidance (planned)

- Run unit + CLI/integration tests on all supported platforms.
- Run WPF tests on Windows runners.
- Publish test results and coverage artifacts.

## Milestone 2 Contract Stability Tests

Milestone 2 adds contract-stability tests that lock down core API expectations shared by the Core, CLI, and WPF layers. These tests are required to prevent accidental breaking changes by:

- Verifying service interfaces keep async + `CancellationToken` signatures.
- Guarding request validation for null/empty critical fields.
- Ensuring response DTOs always expose diagnostics containers.
- Enforcing full `StegoForgeException` and `StegoErrorCode` mapper coverage via reflection/data parity checks.
- Snapshotting key DTO property names used at serialization boundaries for machine-output stability.


## Resolver policy coverage

Carrier format selection behavior is contract-tested in unit tests under `tests/StegoForge.Tests.Unit/Application/CarrierFormatResolverTests.cs` and follows the architecture policy documented in `docs/architecture.md` (see **Carrier format resolver selection policy**).

Required resolver coverage includes:

- single matching handler resolution,
- multiple matching handlers with deterministic precedence selection,
- no matching handlers producing `UnsupportedFormatException`.
