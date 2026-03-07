# Contributing to StegoForge

Thank you for contributing. This guide defines the contribution contract used by roadmap reviews, CI gates, and test expectations.

## Before you start

- Read the architecture and behavior docs before implementing changes:
  - [docs/architecture.md](docs/architecture.md)
  - [docs/testing.md](docs/testing.md)
  - [docs/payload-format.md](docs/payload-format.md)
  - [docs/cli.md](docs/cli.md)
  - [docs/gui.md](docs/gui.md)
  - [docs/roadmap.md](docs/roadmap.md)
- Keep changes scoped to one behavior area whenever possible (format, orchestration, CLI contract, or GUI flow).

## Branch and pull request expectations

- Use a dedicated feature/fix branch for each change; do not commit directly to protected/default branches.
- Keep commits focused and reviewable (avoid mixing unrelated refactors with behavior changes).
- PRs must include:
  - a concise problem statement,
  - a summary of behavioral impact,
  - explicit test evidence (commands + outcomes),
  - docs updated when behavior/contract changes (see [Documentation update requirements](#documentation-update-requirements)).
- If deterministic contracts changed (error code mapping, diagnostics shape, payload header semantics, CLI JSON), call this out explicitly in the PR description.

## Architectural boundaries and coding standards

StegoForge enforces strict Core/Application/UI separation.

### Core (`src/StegoForge.Core`)

- Contains contracts, models, and canonical error taxonomy.
- Must remain delivery-agnostic (no CLI/WPF/UI dependencies).
- Changes to abstractions or `StegoErrorCode` require corresponding tests and documentation updates.

### Application (`src/StegoForge.Application`)

- Owns orchestration policy, provider coordination, and deterministic validation/error mapping.
- Must depend on Core abstractions only (not on CLI/WPF presentation types).
- Compression/encryption ordering and policy validation must remain deterministic and consistent with `docs/architecture.md`.

### UI/Entry points (`src/StegoForge.Cli`, `src/StegoForge.Wpf`)

- Parse input, invoke application services, and render results.
- Must not reimplement domain behavior that belongs in Application/Core.
- User-facing diagnostics must use shared error contracts and remain stable.

### Provider boundaries (`src/StegoForge.Formats`, `src/StegoForge.Crypto`, `src/StegoForge.Compression`)

- Providers implement Core abstractions and remain independently testable.
- Carrier format handlers treat payload envelopes as opaque bytes; crypto/compression concerns stay in application orchestration.
- New providers must expose deterministic metadata/identifiers and fail with typed, mappable exceptions.

## Required tests for behavior changes

Feature or behavior changes are not complete without test updates.

### Minimum expectation

- Update or add tests in the project(s) matching the changed layer:
  - unit tests (`tests/StegoForge.Tests.Unit`) for contracts, providers, and deterministic errors,
  - integration tests (`tests/StegoForge.Tests.Integration`) for cross-layer orchestration,
  - CLI tests (`tests/StegoForge.Tests.Cli`) for command surface, exit codes, and JSON output shape,
  - WPF tests (`tests/StegoForge.Tests.Wpf`) for command-flow and validation behavior when GUI logic changes.
- Include both positive and negative-path coverage for new behavior.
- Regression fixes must include a regression test that fails before the fix and passes after.

### Deterministic errors and diagnostics contract requirements

When changing parsing/orchestration/command behavior, tests must continue to verify:

- typed exception behavior where applicable,
- stable `StegoErrorCode` mapping,
- deterministic CLI exit code and stderr/JSON error shape,
- secret-safe diagnostics (no password/plaintext leakage).

If a contract intentionally changes, update tests and docs in the same PR.

## Adding new format, crypto, or compression providers

All new providers must include implementation + registration + test coverage.

### 1) Core and registration

- Implement the relevant Core abstraction (`ICarrierFormatHandler`, `ICryptoProvider`, or `ICompressionProvider`).
- Register provider in service-collection wiring used by application composition.
- Ensure provider identifiers/format tokens are stable and documented.

### 2) Required test categories

At minimum, add or extend tests for:

- **Contract tests**: interface/metadata expectations, deterministic identifiers.
- **Positive-path tests**: expected success behavior for supported inputs/options.
- **Negative-path tests**: malformed/truncated/unsupported inputs map to deterministic typed errors + `StegoErrorCode`.
- **Boundary/capacity tests** (formats): exact-fit, overflow-by-one, tiny-carrier behavior.
- **Round-trip orchestration tests** (formats/crypto/compression): embed→extract consistency with relevant modes.
- **CLI contract tests** (if observable via CLI): exit code and output contract stability.
- **Hardening tests** where applicable: fuzz/robustness coverage aligned with `docs/testing.md` guidance.

### 3) Documentation updates for new providers

Update docs for any new provider capability:

- architecture/design behavior,
- payload/metadata contract changes,
- CLI/GUI usage surface,
- testing matrix and acceptance criteria.

## Documentation update requirements

Documentation updates are required whenever behavior, contracts, or user workflows change.

Update the relevant files in the same PR (non-exhaustive):

- `README.md` for user-facing capability and quickstart changes,
- `docs/architecture.md` for layer boundaries and runtime flow changes,
- `docs/testing.md` for required/added test strategy or command updates,
- `docs/payload-format.md` for envelope/header/metadata versioning changes,
- `docs/cli.md` for command, option, output, and exit-code changes,
- `docs/gui.md` for WPF workflow/validation/diagnostics changes,
- `docs/roadmap.md` when milestone acceptance criteria or completion evidence changes.

## Keeping roadmap and test conventions aligned

Contribution changes must stay consistent with conventions already enforced in this repository:

- milestone acceptance criteria in `docs/roadmap.md`,
- deterministic behavior contracts verified by test suites in `docs/testing.md`,
- fixed architecture boundaries in `docs/architecture.md`.

If your change introduces a new convention, codify it in tests and documentation immediately.
