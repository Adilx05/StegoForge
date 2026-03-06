# CLI

The CLI app (`src/StegoForge.Cli`) provides scriptable steganography workflows.

## Planned command groups

- `embed` — hide payload data inside a carrier.
- `extract` — recover payload from an encoded carrier.
- `capacity` — estimate available payload size for a given carrier.
- `info` — inspect carrier and embedded metadata (when available).

## Planned option themes

- Input/output file paths.
- Encryption options (password/key, algorithm).
- Compression options.
- Output format (human-readable vs structured).
- Overwrite/force and safety confirmations.

## CLI design principles

- Stable command semantics for automation.
- Deterministic non-zero exit codes for failures.
- Error messages aligned with shared domain error codes.
- Predictable stdout/stderr usage for scripting.

## Planned exit-code mapping

The CLI will map `StegoErrorCode` values to deterministic non-zero process exit codes so automation can branch reliably.

| `StegoErrorCode` | Planned CLI exit code | Notes |
| --- | --- | --- |
| `FileNotFound` | `2` | Input file path issue (missing/unreadable). |
| `InvalidArguments` | `3` | Invalid/missing arguments or invalid option combinations. |
| `CorruptedData` | `4` | Encoded data is malformed/truncated/invalid. |
| `UnsupportedFormat` | `5` | Carrier or payload format not supported. |
| `InvalidPayload` | `6` | Payload validation failure before/while processing, including corrupted/truncated compressed payload data during decompression. |
| `InvalidHeader` | `7` | Embedded header is invalid or incompatible. |
| `WrongPassword` | `8` | Authentication/decryption failed due to secret mismatch. |
| `InsufficientCapacity` | `9` | Carrier capacity is too small for requested operation. |
| `OutputAlreadyExists` | `10` | Output path exists and overwrite is disallowed. |
| `InternalProcessingFailure` | `1` | Unexpected internal failure; details available in diagnostics/verbose output. |

Exit code `0` remains reserved for success.

## Example target invocations (planned)

```bash
stegoforge embed --carrier in.png --payload secret.bin --out out.png --encrypt --password "..."
stegoforge extract --carrier out.png --out recovered.bin --password "..."
stegoforge capacity --carrier in.png
stegoforge info --carrier out.png
```

## Current status

- Baseline CLI project is present with placeholder entry point.
- Full command surface is planned in roadmap milestones.
