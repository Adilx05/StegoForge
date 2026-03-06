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

**Acceptance criteria:**

- Envelope serializer/deserializer supports magic, version, flags, header, payload.
- Truncation/corruption handling returns deterministic errors.
- Round-trip tests validate binary compatibility expectations.

## Milestone 4 — Compression provider integration

**Goal:** Add pluggable compression for payload preprocessing.

**Acceptance criteria:**

- At least one compression implementation (e.g., Deflate/Brotli).
- Compression can be toggled via embed request options.
- Decompression failures are surfaced with precise errors.

## Milestone 5 — Crypto provider integration

**Goal:** Add authenticated encryption and key derivation support.

**Acceptance criteria:**

- AEAD-based encryption path implemented.
- Password/key derivation is configurable through request options.
- Wrong key and tamper scenarios fail with clear, test-covered errors.

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
