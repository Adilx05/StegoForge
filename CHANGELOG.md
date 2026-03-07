# Changelog

All notable changes to this project are documented in this file.

The format is based on Keep a Changelog principles, with release versions tracked as `vX.Y.Z` tags and `X.Y.Z` changelog headings.

## [Unreleased]

### Added
- Release governance documentation in `README.md` and `docs/building.md`.
- Initial maintainer release template at `.github/release-template.md`.
- Release workflow metadata validation for tag/version/changelog summary.

### Changed
- Release workflow now requires explicit release metadata input and validates tag/version/changelog consistency.

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
