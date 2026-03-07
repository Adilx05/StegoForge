# CLI

The CLI app (`src/StegoForge.Cli`) provides scriptable steganography workflows.

## Planned command groups

- `embed` — hide payload data inside a carrier.
- `extract` — recover payload from an encoded carrier.
- `capacity` — estimate available payload size for a given carrier.
- `info` — inspect carrier and embedded metadata (when available).

## Encryption and KDF option surface (Milestone 5)

### Common embed options

| Option | Description |
| --- | --- |
| `--encrypt <required|optional|off>` | Controls encryption policy. `required` fails if crypto settings are incomplete; `optional` uses defaults when password source is available; `off` disables encryption metadata/processing. |
| `--cipher <id>` | AEAD algorithm id (for example `aes-256-gcm`). |
| `--kdf <id>` | KDF algorithm id (for example `pbkdf2-sha256`, `argon2id`). |
| `--kdf-iterations <n>` | PBKDF2-style iteration count (when selected KDF supports it). |
| `--kdf-memory-kib <n>` | Argon2-style memory cost (KiB), KDF-specific. |
| `--kdf-parallelism <n>` | Argon2-style parallelism lanes, KDF-specific. |
| `--salt-length <bytes>` | Requested generated salt length in bytes. |
| `--nonce-length <bytes>` | Requested generated AEAD nonce length in bytes (if algorithm permits configurable length). |

### Password source options

| Option | Description | Guidance |
| --- | --- | --- |
| `--password-prompt` | Read password from secure interactive prompt. | Preferred for manual use; avoids shell history leakage. |
| `--password-env <VAR_NAME>` | Read password from environment variable. | Use only in controlled CI/runtime environments and clear variable after use. |
| `--password-file <path>` | Read password bytes from file. | Restrict file permissions (`0600` equivalent) and avoid syncing this file. |
| `--password-stdin` | Read password from stdin pipe. | Preferred for automation with secret managers (`printf ... | stegoforge ...`). |

Do **not** pass raw secrets directly on the command line (for example `--password "..."`) because command-line arguments are often visible in process listings and shell history.

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
stegoforge embed --carrier in.png --payload secret.bin --out out.png \
  --encrypt required --cipher aes-256-gcm --kdf pbkdf2-sha256 --kdf-iterations 600000 \
  --password-prompt

stegoforge embed --carrier in.png --payload secret.bin --out out.png \
  --encrypt required --cipher aes-256-gcm --kdf argon2id --kdf-memory-kib 65536 --kdf-parallelism 2 \
  --password-env STEGOFORGE_PASSWORD

printf '%s' "$STEGOFORGE_PASSWORD" | stegoforge extract --carrier out.png --out recovered.bin \
  --password-stdin

stegoforge capacity --carrier in.png
stegoforge info --carrier out.png

# WAV carrier examples (v1)
stegoforge embed --carrier in.wav --payload secret.bin --out out.wav \
  --encrypt off

stegoforge extract --carrier out.wav --out recovered.bin

stegoforge capacity --carrier in.wav
```

## WAV v1 limitations

- WAV support is currently limited to `wav-lsb-v1` carrier constraints used by `WavLsbFormatHandler`:
  - RIFF/WAVE container with required `fmt` and `data` chunks.
  - `fmt` format tag `1` (PCM) only.
  - 16-bit little-endian samples only.
  - Mono or stereo channel layouts only.
- Non-PCM, unsupported bit depths, and malformed/missing required chunks are rejected deterministically as unsupported-format or invalid-header failures.

## Current status

- Baseline CLI project is present with placeholder entry point.
- Full command surface is planned in roadmap milestones.
