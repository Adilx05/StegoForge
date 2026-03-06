# Building

## Prerequisites

- .NET SDK 9.0+ (see `global.json`)
- Git
- Windows required for WPF build/test workflows

## Restore once

```bash
 dotnet restore StegoForge.sln
```

## CLI-only build/test

```bash
# Build CLI app
 dotnet build src/StegoForge.Cli/StegoForge.Cli.csproj

# Run CLI tests
 dotnet test tests/StegoForge.Tests.Cli/StegoForge.Tests.Cli.csproj

# Optional: run unit tests frequently with CLI work
 dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj
```

## WPF-only build/test

> Recommended on Windows with desktop workloads installed.

```bash
# Build WPF app
 dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj

# Run WPF tests
 dotnet test tests/StegoForge.Tests.Wpf/StegoForge.Tests.Wpf.csproj
```

## Full-solution build/test

```bash
# Build all projects
 dotnet build StegoForge.sln

# Run complete test suite
 dotnet test StegoForge.sln
```

## Useful diagnostics

```bash
# Build with normal verbosity for troubleshooting
 dotnet build StegoForge.sln -v normal

# List installed SDKs
 dotnet --list-sdks
```

## Notes

- If WPF targets fail on non-Windows hosts, build/test CLI/core/integration projects separately.
- Keep local SDK aligned with `global.json` to avoid restore/build drift.
