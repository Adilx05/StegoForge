# CLI

The CLI app (`src/StegoForge.Cli`) provides scriptable steganography workflows.

## Canonical syntax

```bash
stegoforge <command> [options]
```

Commands:

- `embed`
- `extract`
- `capacity`
- `info`
- `version`
- `help` (alias to root `--help`)

## Commands and examples

### `embed`

Syntax:

```bash
stegoforge embed --carrier <carrier-path> --payload <payload-path> --out <output-path> [--encrypt none|optional|required] [--compress off|auto|on] [--password <value>] [--json] [--quiet|--verbose]
```

Examples:

```bash
stegoforge embed --carrier in.png --payload secret.bin --out out.png
stegoforge embed --carrier in.png --payload secret.bin --out out.png --encrypt required --password "correct horse"
stegoforge embed --carrier in.png --payload secret.bin --out out.png --compress auto --json
```

### `extract`

Syntax:

```bash
stegoforge extract --carrier <carrier-path> --out <output-path> [--encrypt none|optional|required] [--compress off|auto|on] [--password <value>] [--json] [--quiet|--verbose]
```

Examples:

```bash
stegoforge extract --carrier out.png --out recovered.bin
stegoforge extract --carrier out.png --out recovered.bin --password "correct horse"
stegoforge extract --carrier out.png --out recovered.bin --json
```

### `capacity`

Syntax:

```bash
stegoforge capacity --carrier <carrier-path> --payload <bytes> [--encrypt none|optional|required] [--compress off|auto|on] [--json] [--quiet|--verbose]
```

Examples:

```bash
stegoforge capacity --carrier in.png --payload 1024
stegoforge capacity --carrier in.wav --payload 65536 --compress auto
stegoforge capacity --carrier in.png --payload 2048 --json
```

### `info`

Syntax:

```bash
stegoforge info --carrier <carrier-path> [--encrypt none|optional|required] [--compress off|auto|on] [--json] [--quiet|--verbose]
```

Examples:

```bash
stegoforge info --carrier out.png
stegoforge info --carrier out.png --json
```

### `version` and `help`

```bash
stegoforge version
stegoforge version --json
stegoforge help
stegoforge --help
```

## Output modes

All commands support:

- **Human-readable text** (default) on `stdout` for success.
- **JSON** (`--json`) on `stdout` for success.
- Errors on `stderr` with deterministic non-zero exit codes.

### JSON contracts

`info --json` success shape:

```json
{
  "command": "info",
  "formatId": "png-lsb-v1",
  "formatDetails": {
    "formatId": "png-lsb-v1",
    "displayName": "PNG LSB",
    "handlerVersion": "1.0"
  },
  "carrierSizeBytes": 524288,
  "estimatedCapacityBytes": 65536,
  "availableCapacityBytes": 65536,
  "embeddedDataPresent": false,
  "supportsEncryption": true,
  "supportsCompression": true,
  "payloadMetadata": null,
  "protectionDescriptors": {
    "compressionDescriptor": "none",
    "encryptionDescriptor": "none",
    "integrityDescriptor": "none"
  },
  "diagnostics": {
    "warnings": [],
    "notes": [],
    "duration": "00:00:00",
    "algorithmIdentifier": null,
    "providerIdentifier": null
  }
}
```

`capacity --json` success shape:

```json
{
  "command": "capacity",
  "carrierFormatId": "png-lsb-v1",
  "requestedPayloadSizeBytes": 2048,
  "availableCapacityBytes": 65536,
  "maximumCapacityBytes": 65536,
  "safeUsableCapacityBytes": 64000,
  "estimatedOverheadBytes": 48,
  "canEmbed": true,
  "remainingBytes": 61952,
  "failureReason": null,
  "constraintBreakdown": [],
  "diagnostics": {
    "warnings": [],
    "notes": [],
    "duration": "00:00:00",
    "algorithmIdentifier": null,
    "providerIdentifier": null
  }
}
```

#### Error object shape (`--json`)

For command failures (including parser errors), JSON mode writes this object to `stderr`:

```json
{
  "type": "error",
  "exitCode": 9,
  "code": "InsufficientCapacity",
  "message": "Requested payload exceeds available capacity. Requested=100 bytes, available=10 bytes."
}
```

This shape is stable for automation.


### JSON mode contract notes

- On **successful command execution** with `--json`, the CLI writes a single JSON object to `stdout` and returns exit code `0`.
- On **command failure** with `--json`, the CLI writes a single error JSON object to `stderr` and returns a non-zero mapped exit code.
- On **parser/validation failures** with `--json` (for example missing required options or invalid enum values), the same JSON error shape is emitted to `stderr` with exit code `3` (`InvalidArguments`).
- JSON mode is intended for automation: consumers should branch by process exit code, then parse `stdout` on success and `stderr` on failure.

## Automation notes

- Check process exit code first.
- On success with `--json`, parse `stdout`.
- On failure with `--json`, parse `stderr` error object.
- `capacity.failureReason` and `capacity.constraintBreakdown` provide machine-readable constraint detail when embedding is not possible.

Example automation flow:

```bash
if stegoforge capacity --carrier in.png --payload 2048 --json >out.json 2>err.json; then
  jq '.canEmbed' out.json
else
  jq '.code, .message' err.json
fi
```

## Exit-code table

| Condition | CLI exit code |
| --- | --- |
| Success | `0` |
| `StegoErrorCode.InternalProcessingFailure` | `1` |
| `StegoErrorCode.FileNotFound` | `2` |
| `StegoErrorCode.InvalidArguments` (including parser errors) | `3` |
| `StegoErrorCode.CorruptedData` | `4` |
| `StegoErrorCode.UnsupportedFormat` | `5` |
| `StegoErrorCode.InvalidPayload` | `6` |
| `StegoErrorCode.InvalidHeader` | `7` |
| `StegoErrorCode.WrongPassword` | `8` |
| `StegoErrorCode.InsufficientCapacity` | `9` |
| `StegoErrorCode.OutputAlreadyExists` | `10` |
