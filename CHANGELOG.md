# Changelog

All notable changes to this project are documented in this file.

The format is based on Keep a Changelog principles, with release versions tracked as `vX.Y.Z` tags and `X.Y.Z` changelog headings.

## [Unreleased]

### Added
- Release governance documentation in `README.md` and `docs/building.md`.
- Initial maintainer release template at `.github/release-template.md`.
- Release workflow metadata validation for tag/version/changelog summary.

### Changed
- Added shared MinVer-based version foundation (`Directory.Build.props` + `Directory.Build.targets`) with explicit `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` mapping across all projects.
- Release workflow now requires explicit release metadata input and validates tag/version/changelog consistency.
- Milestone 14 release-readiness documentation verification pass completed for `README.md` and `docs/` with timestamp `2026-03-07T00:00:00Z`, including CLI command snippet, supported-format scope (PNG/BMP/WAV), and CI/release mapping validation.

### Migration notes
- None.

## [1.0.0] - TBD

### Added
- Initial stable release baseline for StegoForge core services, format handlers, CLI, and WPF application.

### Changed
- None.

### Fixed
- None.

### Security
- None.

### Migration notes
- Document any upgrade path items in bullets:
  - **Who is impacted:** <consumer/operator scope>
  - **Action required:** <required change or `None`>
  - **Rollback guidance:** <how to revert safely>
