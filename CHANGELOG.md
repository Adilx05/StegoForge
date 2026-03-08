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

### Fixed
- Fixed MinVer metadata mapping to ensure WPF/WindowsDesktop builds always receive valid numeric `AssemblyVersion` and `FileVersion` values.

### Migration notes
- None.

## [1.0.1] - 2026-03-08

### Added
- Release workflow now publishes a Windows CLI archive (`stegoforge-cli-<tag>-windows-x64.zip`) alongside Linux CLI artifacts.

### Changed
- `.github/workflows/release.yml` `package-cli` job now uses a platform matrix (`linux-x64`, `windows-x64`) with explicit RID publish commands (`linux-x64`, `win-x64`) and platform-specific output directories.
- Windows CLI packaging now uses `Get-FileHash` for SHA-256 manifest generation and includes cosign signing/verification parity with Linux CLI packaging.
- Release asset upload/download and publish file lists now include Windows CLI archive, checksum, and signature assets.
- `docs/building.md` release sections now document Windows CLI required assets and verification commands.

### Fixed
- CLI release artifact naming is now aligned to real target platforms (`linux-x64`, `windows-x64`) for deterministic release attachments.

### Security
- Extended cosign integrity chain to Windows CLI archive + checksum artifacts, including verification before upload.

### Migration notes
- **Who is impacted:** Release operators and downstream users consuming CLI binaries.
- **Action required:** Download and verify the platform-specific CLI artifact (`linux-x64` tarball or `windows-x64` zip) for `v1.0.1`.
- **Rollback guidance:** Re-run release workflow for prior tag (`v1.0.0`) and consume previous CLI assets if needed.

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
