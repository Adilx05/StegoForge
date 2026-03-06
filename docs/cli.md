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
