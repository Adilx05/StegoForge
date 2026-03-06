# Payload Format (Planned)

This document defines the planned StegoForge payload envelope for embedding into carrier formats.

## Objectives

- Versioned and backward-compatible.
- Supports optional compression and encryption.
- Includes integrity/authentication information.
- Efficient enough for constrained carrier capacities.

## Logical envelope structure

```text
+---------------------------+
| Magic (4 bytes)          |
+---------------------------+
| Version (1 byte)         |
+---------------------------+
| Flags (1 byte)           |
+---------------------------+
| HeaderLength (2 bytes)   |
+---------------------------+
| Header (variable)        |
+---------------------------+
| PayloadLength (8 bytes)  |
+---------------------------+
| Payload (variable)       |
+---------------------------+
| AuthTag/Checksum (var)   |
+---------------------------+
```

## Header fields (candidate)

- `ContentType` (UTF-8 string; optional)
- `OriginalFileName` (UTF-8 string; optional)
- `CreatedUtc` (unix epoch milliseconds)
- `CompressionAlgorithm` (`none`, `deflate`, `brotli`, ...)
- `EncryptionAlgorithm` (`none`, `aes-gcm-256`, ...)
- `KdfAlgorithm` (`none`, `argon2id`, `pbkdf2`)
- `Nonce/IV`
- `Salt`
- `AdditionalAuthenticatedData` (optional)

## Flags (candidate bit layout)

- bit 0: payload compressed
- bit 1: payload encrypted
- bit 2: metadata present
- bit 3: multipart payload (future)
- bit 4: reserved
- bit 5: reserved
- bit 6: reserved
- bit 7: reserved

## Processing order

### Embed

1. Build header metadata.
2. Compress payload (if enabled).
3. Encrypt compressed payload + selected header fields (if enabled).
4. Compute authentication tag/checksum.
5. Serialize envelope and pass to format handler.

### Extract

1. Parse magic/version and validate support.
2. Parse header and flags.
3. Verify authentication/checksum.
4. Decrypt (if encrypted).
5. Decompress (if compressed).
6. Emit payload + metadata.

## Versioning strategy

- Magic identifies StegoForge payloads.
- Version increments for incompatible wire changes.
- Minor additive header fields are length-prefixed and skippable.
- Unsupported version must return a specific `UnsupportedPayloadVersion`-style error.

## Security notes

- Prefer authenticated encryption (AEAD) rather than encrypt-then-raw-checksum.
- Never reuse nonce+key pairs.
- Avoid leaking sensitive metadata in plaintext when encryption is enabled.
- Resist malformation attacks with strict bounds checking during decode.


## Payload-derived metadata surfaced by API models

When envelope metadata is readable, `CarrierInfoResponse` and `ExtractResponse` surface normalized metadata to callers:

- `OriginalFileName` (nullable): source filename from payload header.
- `OriginalSizeBytes` (nullable): original pre-processing byte length.
- `CreatedUtc` (nullable): payload creation timestamp.
- `HeaderVersion` (required, non-negative): decoded payload-header version.
- `CompressionDescriptor`, `EncryptionDescriptor`, `IntegrityDescriptor`: human-readable algorithm descriptors (`none` when absent).

These values are descriptive API fields only; they do not replace cryptographic verification. Integrity status is reported separately on extraction via `IntegrityVerificationResult`.
