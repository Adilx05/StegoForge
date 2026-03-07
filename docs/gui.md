# GUI (WPF)

The WPF app (`src/StegoForge.Wpf`) is the desktop user interface for StegoForge.

## Planned capabilities

- Guided embed workflow:
  - Select carrier file
  - Select payload file/text
  - Configure compression/encryption options
  - Choose output path and run
- Guided extract workflow:
  - Select encoded carrier
  - Provide key/passphrase (if required)
  - Choose destination path
- Capacity and info views for quick validation before embedding.

## UX principles

- Clear defaults with explicit advanced settings.
- Deterministic, human-readable error feedback.
- No silent fallback behavior for security-sensitive options.
- Progress indication for long-running operations.

## Architectural guidance

- Prefer MVVM boundaries for maintainability and testability.
- Keep business logic in application/core services, not code-behind.
- Surface domain error codes as actionable UI messages.

## Validation consistency and deterministic shared-service errors

GUI validation and error messaging should remain consistent with CLI outcomes by routing operations through the same application services/policy components instead of UI-only validation logic.

User-facing guarantees:

- Invalid configuration combinations fail fast before carrier handler I/O with deterministic `InvalidArguments` semantics.
- Existing-output protection behaves deterministically (`OutputAlreadyExists`) when overwrite is disallowed.
- Format resolution and unsupported-carrier failures are deterministic due to shared resolver precedence.
- Wrong-password, payload/header corruption, and insufficient-capacity outcomes preserve stable domain error codes for actionable UI messages.

This behavior is covered by shared orchestration/policy tests:

- `tests/StegoForge.Tests.Integration/ApplicationServiceOrchestrationIntegrationTests.cs`
- `tests/StegoForge.Tests.Integration/OrchestrationConsistencyIntegrationTests.cs`
- `tests/StegoForge.Tests.Unit/Application/OperationPolicyValidatorTests.cs`
- `tests/StegoForge.Tests.Unit/Application/CarrierFormatResolverTests.cs`


## Current status

- Project scaffolding and baseline startup are in place.
- Full UX and command flows are planned in roadmap milestones.
