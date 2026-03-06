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
