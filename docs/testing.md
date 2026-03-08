# Testing Strategy

_Last verified against source: 2026-03-07 (`0fd7c07`)._
## Test project map

- `tests/StegoForge.Tests.Unit`
  - Fast tests for domain models, interfaces, error mapping, and provider behavior with stubs/mocks.
- `tests/StegoForge.Tests.Integration`
  - Cross-assembly tests for real embed/extract flows and service composition.
- `tests/StegoForge.Tests.Cli`
  - CLI command contract tests and exit-code behavior.
- `tests/StegoForge.Tests.Wpf`
  - WPF-level smoke tests where environment permits.

## Quality gates (current)

- All test projects pass on CI.
- Build warnings are reviewed and kept low/noise-free.
- New handlers/providers include both positive and negative-path coverage.
- Regression tests accompany fixed defects.

## Recommended local commands

### CLI-only / Core tests (cross-platform)

```bash
dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release
```

### WPF-only tests (Windows-specific)

```powershell
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --filter "FullyQualifiedName~WpfCommandFlowTests"
```

### Full-solution tests (Windows-specific)

> `StegoForge.sln` includes WPF projects and should be tested on Windows.

```powershell
dotnet test StegoForge.sln --configuration Release
```

## Critical test matrix (release readiness)

The following suites are **release-critical**. A release candidate is only considered ready when each suite has run under its required trigger and passed.

| Required suite | Trigger | CI workflow/job mapping | Required CI evidence |
| --- | --- | --- | --- |
| Unit tests (`StegoForge.Tests.Unit`) | Every PR/push and release cut | `.github/workflows/ci.yml` → `core-cli` (matrix: `ubuntu-latest`, `windows-latest`) → `Test core/CLI projects` | Both matrix lanes green with unit suite included in run log/artifacts |
| Integration tests (`StegoForge.Tests.Integration`) | Every PR/push and release cut | `.github/workflows/ci.yml` → `core-cli` (matrix: `ubuntu-latest`, `windows-latest`) → `Test core/CLI projects` | Both matrix lanes green with integration suite included in run log/artifacts |
| CLI tests (`StegoForge.Tests.Cli`) | Every PR/push and release cut | `.github/workflows/ci.yml` → `core-cli` (matrix: `ubuntu-latest`, `windows-latest`) → `Test core/CLI projects` | Both matrix lanes green with CLI suite included in run log/artifacts |
| WPF tests (`StegoForge.Tests.Wpf`) | Every PR/push and release cut | `.github/workflows/ci.yml` → `wpf` → `Test WPF smoke project` and `Test WPF command-flow subset` | `wpf` job green on `windows-latest` |
| Hardening suite (bounded) | PR/push/release validation (`github.event_name != 'schedule'`) | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (bounded; PR/push)` and `.github/workflows/ci.yml` → `wpf` → `Test WPF hardening subset (Windows)` | No failures in bounded hardening steps; artifacts/repros reviewed when produced |
| Hardening suite (full fuzz campaigns) | Scheduled/nightly (`github.event_name == 'schedule'`) | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (full fuzz campaigns; nightly/scheduled)` | Latest scheduled run green for full hardening campaigns |

### Release cut failure conditions

Do **not** cut/publish a release when any of the following conditions hold:

- Any critical suite above is skipped for the trigger where it is required.
- `Hardening suite (bounded; PR/push)` (or the WPF hardening subset step) has any failure.
- The Windows WPF lane (`wpf` job) is not green.
- Required CI status checks are not successful for the commit being released.

### Release Candidate (RC) checklist (before tagging)

Run the following workflow and command evidence checklist on the exact commit that will receive the tag:

- [ ] Confirm `CHANGELOG.md` has the target version section and migration notes.
  - Evidence: diff/review of `CHANGELOG.md` for `## X.Y.Z` section.
- [ ] Confirm the CI workflow `.github/workflows/ci.yml` is green for required jobs.
  - Evidence: successful run for `core-cli` (both `ubuntu-latest` and `windows-latest`) and `wpf` (`windows-latest`).

Execute (or verify equivalent CI execution records for) the following commands:

```bash
dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --filter "Category=Hardening&Campaign!=Fuzz-Full"
dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --filter "Category=Hardening&Campaign!=Fuzz-Full"
```

```powershell
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --filter "FullyQualifiedName~WpfCommandFlowTests"
dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --filter "Category=Hardening"
```

- [ ] Capture workflow evidence required before creating a release tag:
  - [ ] `.github/workflows/ci.yml` run URL attached to release notes draft.
  - [ ] Uploaded CI artifacts present: `test-results-core-cli-ubuntu-latest`, `test-results-core-cli-windows-latest`, `test-results-wpf-windows-latest`.
  - [ ] No failed steps in `Hardening suite (bounded; PR/push)` and `Test WPF hardening subset (Windows)`.
- [ ] Confirm release workflow readiness in `.github/workflows/release.yml` inputs:
  - [ ] `tag` format `vX.Y.Z`.
  - [ ] `version` format `X.Y.Z` where `tag == v{version}`.
  - [ ] `changelog_summary` is non-empty and matches `CHANGELOG.md` highlights.

Only create the Git tag after all checklist items above are complete.

### CI mapping for documented test commands

| Documented command | CI workflow job/step |
| --- | --- |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release` + integration + CLI equivalents | `.github/workflows/ci.yml` → `core-cli` → `Test core/CLI projects` |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --filter "Category=Hardening&Campaign!=Fuzz-Full"` + integration equivalent | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (bounded; PR/push)` |
| `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --filter "Category=Hardening&Campaign=Fuzz-Full"` + integration equivalent | `.github/workflows/ci.yml` → `core-cli` → `Hardening suite (full fuzz campaigns; nightly/scheduled)` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release` | `.github/workflows/ci.yml` → `wpf` → `Test WPF smoke project` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --filter "FullyQualifiedName~WpfCommandFlowTests"` | `.github/workflows/ci.yml` → `wpf` → `Test WPF command-flow subset` |
| `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --filter "Category=Hardening"` | `.github/workflows/ci.yml` → `wpf` → `Test WPF hardening subset (Windows)` |

Environment caveats:

- WPF tests require Windows CI runners or local Windows machines.
- Full hardening fuzz campaigns are scheduled in CI because they are intentionally long-running.

## Docs QA checklist (pre-release)

Before a release cut (or milestone-docs sign-off), maintainers should run this docs-focused checklist to keep documentation accurate and CI-aligned:

- Verify scope docs are present and current: `README.md`, `CONTRIBUTING.md`, `docs/architecture.md`, `docs/payload-format.md`, `docs/building.md`, `docs/testing.md`, `docs/cli.md`, `docs/gui.md`, and `docs/roadmap.md`.
- Confirm every command block still maps to current workflow names/steps in `.github/workflows/ci.yml` and `.github/workflows/release.yml`.
- Re-run core documentation-linked test suites locally (or validate equivalent CI runs):
  - `dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj --configuration Release --no-build`
  - `dotnet test tests/StegoForge.Tests.Integration/StegoForge.Tests.Integration.csproj --configuration Release --no-build`
  - `dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj --configuration Release --no-build`
  - On Windows: `dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj --configuration Release --no-build`
- Verify CI checks are green for documentation-linked jobs before release:
  - `.github/workflows/ci.yml`: `core-cli`, `wpf`
  - `.github/workflows/release.yml`: `package-cli`, `package-wpf` (for tag/release validation)
- CI-doc alignment check: confirm the exact job/step names referenced in this file still match `.github/workflows/ci.yml` (`core-cli`, `wpf`, `Test core/CLI projects`, `Hardening suite (bounded; PR/push)`, `Hardening suite (full fuzz campaigns; nightly/scheduled)`, `Test WPF smoke project`, `Test WPF command-flow subset`, `Test WPF hardening subset (Windows)`).
- Ensure milestone status language is factual (checked items completed, pending work left unchecked) in `docs/roadmap.md`.


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

## Performance and stress checks (backlog)

- Capacity boundary tests (near-max payloads).
- Large-file memory profile checks.
- Multi-file batch throughput benchmarking.

## CI hardening strategy

CI now separates baseline correctness runs from hardening-focused runs so pull requests stay deterministic while nightly schedules can run deeper campaigns.

### Baseline jobs (all PRs/pushes)

- `core-cli` matrix (`ubuntu-latest`, `windows-latest`) runs full unit, integration, and CLI suites.
- `wpf` (`windows-latest`) runs WPF smoke + command-flow tests.

### Hardening jobs in CI

- **Bounded hardening segment (PR/push):**
  - Unit + integration tests filtered with `Category=Hardening&Campaign!=Fuzz-Full`.
  - This includes deterministic serializer/handler fuzz subsets and hardening regression coverage.
- **WPF hardening subset (Windows):**
  - WPF tests filtered with `Category=Hardening` to validate GUI-layer sanitization/robustness on the Windows runner.
- **Full fuzz segment (scheduled/nightly):**
  - Unit + integration tests filtered with `Category=Hardening&Campaign=Fuzz-Full`.
  - Intended for longer-running deterministic fuzz campaigns that are too expensive for PR latency.

### Trait conventions for hardening campaigns

Use xUnit traits on hardening tests so CI routing remains explicit:

- `Category=Hardening` — marks tests that are part of the hardening strategy.
- `Campaign=Fuzz-Bounded` — deterministic, short-running fuzz subset suitable for PRs.
- `Campaign=Fuzz-Full` + `Execution=Nightly` — long-running campaign reserved for scheduled workflows.

### Hardening artifact capture

CI sets `STEGOFORGE_HARDENING_ARTIFACTS_DIR` for hardening test segments.
When an unexpected exception is encountered during fuzz loops, tests persist minimal repro artifacts to that folder (raw failing bytes + seed/exception metadata).
The workflow uploads:

- `TestResults/*.trx`
- `TestResults/hardening-repros/*`

This provides immediate corpus/repro seed evidence for post-failure debugging without rerunning entire campaigns locally.

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

## Hardening and cancellation test guidance

The hardening baseline now includes configurable processing-limit and cancellation-path coverage.

### Default limits under test

The default `ProcessingLimits` contract is:

- `MaxPayloadBytes = 16 MiB`
- `MaxHeaderBytes = 64 KiB`
- `MaxEnvelopeBytes = 20 MiB`
- `MaxCarrierSizeBytes = 128 MiB` (nullable)

When adding tests that exceed these values, construct handlers/services/serializers with explicit low limits so tests remain deterministic and lightweight.

### Required hardening scenarios

- Oversized payload rejection happens before compression/encryption/decompression provider calls.
- Oversized or malformed envelope length fields are rejected before any large allocations.
- Format handlers reject over-limit envelope payloads before doing deep embed/extract work.
- Pre-canceled `CancellationToken` inputs consistently throw `OperationCanceledException` in async format operations.

### Tuning strategy for CI and fuzzing

- Keep CI limits conservative (low memory overhead) and add dedicated stress jobs for larger thresholds.
- For fuzzing, keep `MaxEnvelopeBytes` low enough to prevent accidental large allocation attempts from mutated length prefixes.
- Validate error determinism by asserting both typed exceptions and mapped `StegoErrorCode` values when applicable.


## Milestone 14 documentation verification

Release-readiness documentation is verified for the current source state:

- Verified timestamp: **2026-03-07T00:00:00Z**
- Scope: `README.md`, `docs/architecture.md`, `docs/building.md`, `docs/testing.md`, `docs/cli.md`, `docs/gui.md`, `docs/payload-format.md`, `docs/roadmap.md`
- Confirmation: command snippets, CI/release workflow mapping, and supported-format statements align with current PNG/BMP/WAV implementation scope.
