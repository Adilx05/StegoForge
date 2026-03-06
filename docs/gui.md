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

## Current status

- Project scaffolding and baseline startup are in place.
- Full UX and command flows are planned in roadmap milestones.
