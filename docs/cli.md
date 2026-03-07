# CLI

The CLI app (`src/StegoForge.Cli`) provides scriptable steganography workflows.

## Commands

- `embed` — hide payload data inside a carrier.
- `extract` — recover payload from an encoded carrier.
- `capacity` — estimate available payload size for a given carrier.
- `info` — inspect carrier and embedded metadata (when available).

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

## Exit-code mapping

| `StegoErrorCode` | CLI exit code |
| --- | --- |
| `FileNotFound` | `2` |
| `InvalidArguments` | `3` |
| `CorruptedData` | `4` |
| `UnsupportedFormat` | `5` |
| `InvalidPayload` | `6` |
| `InvalidHeader` | `7` |
| `WrongPassword` | `8` |
| `InsufficientCapacity` | `9` |
| `OutputAlreadyExists` | `10` |
| `InternalProcessingFailure` | `1` |

Exit code `0` is reserved for success.
