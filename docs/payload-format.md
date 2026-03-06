# Payload Format (v1)

This document defines the finalized StegoForge payload envelope for embedding into carrier formats.

## v1 wire layout

All multi-byte integer fields in v1 use **little-endian** byte order.

| Field | Size (bytes) | Description |
|---|---:|---|
| Magic | 4 | Fixed ASCII bytes: `SGF1` |
| Version | 1 | Envelope version. v1 value is `0x01`. |
| Flags | 1 | Bit-field defined in `EnvelopeFlags`. |
| HeaderLength | 2 | Unsigned length of `Header` block. |
| Header | HeaderLength | Structured metadata block. |
| PayloadLength | 8 | Unsigned length of `Payload` block. |
| Payload | PayloadLength | Raw or processed payload bytes per flags/descriptors. |
| IntegrityLength | 2 | Unsigned length of integrity/authentication data. |
| IntegrityData | IntegrityLength | Authentication tag, MAC, checksum, or empty when not used. |

## Header fields (v1)

Header serialization is deterministic and length-prefixed per field by the serializer.

| Field | Type | Notes |
|---|---|---|
| `OriginalFileName` | UTF-8 string (optional) | Empty/absent indicates unknown. |
| `OriginalSizeBytes` | Int64 | Original pre-processing size in bytes. |
| `CreatedUtc` | Int64 | Unix epoch milliseconds. |
| `CompressionDescriptor` | UTF-8 string | `none`, `deflate`, `brotli`, etc. |
| `EncryptionDescriptor` | UTF-8 string | `none`, `aes-gcm-256`, etc. |
| `SaltMetadata` | UTF-8 string (optional) | Encoded salt metadata when encryption/KDF uses one. |
| `NonceMetadata` | UTF-8 string (optional) | Encoded nonce/IV metadata when required. |

## Flags (v1 bit layout)

- bit 0: payload compressed
- bit 1: payload encrypted
- bit 2: optional metadata present
- bits 3-7: reserved for future versions (must not be repurposed by v1)

## Processing order

### Embed

1. Build header metadata.
2. Compress payload (if enabled).
3. Encrypt compressed payload + selected header fields (if enabled).
4. Compute integrity/authentication data.
5. Serialize envelope and pass to format handler.

### Extract

1. Parse magic/version and validate support.
2. Parse flags, header, payload, and integrity data.
3. Verify integrity/authentication data.
4. Decrypt (if encrypted).
5. Decompress (if compressed).
6. Emit payload + metadata.

## Versioning strategy

- Magic identifies StegoForge payloads.
- Version increments for incompatible wire changes.
- Unknown version values must return an unsupported-version error.
- Reserved flag bits are preserved for future expansion.

## Binary compatibility contract

The v1 envelope wire format is protected by fixture/golden tests in `PayloadEnvelopeSerializerTests`:

- `Serialize_KnownFixtureEnvelope_ProducesExpectedGoldenBytes` locks serializer output to a canonical byte sequence.
- `Deserialize_KnownFixtureEnvelope_ProducesExpectedValues` ensures parser compatibility with the same canonical bytes.

When refactoring serializer/parser internals, these tests act as a compatibility contract and must continue passing unless a deliberate versioned wire-format change is introduced.

## Security notes

- Prefer authenticated encryption (AEAD) rather than encrypt-then-raw-checksum.
- Never reuse nonce+key pairs.
- Avoid leaking sensitive metadata in plaintext when encryption is enabled.
- Resist malformation attacks with strict bounds checking during decode.


## Parser rules (strict decode contract)

Deserializer implementations must follow a strict, deterministic parse strategy:

1. Perform bounds checks **before every read** (single-byte or length-prefixed).
2. Validate fixed prefix fields in order: `Magic`, `Version`, then `Flags`.
3. Reject any v1 `Flags` value that sets reserved bits 3-7.
4. Read each declared length (`HeaderLength`, `PayloadLength`, `IntegrityLength`) and reject when the declared size exceeds remaining bytes.
5. Parse header with schema byte `0x01` for v1 and reject unknown schema identifiers.
6. Reject envelopes that contain trailing bytes after `IntegrityData`.
7. Reject header blocks that contain trailing bytes after all defined header fields are read.

## Deterministic failure mapping

To keep CLI/GUI handling stable, parser failures should map to StegoForge typed exceptions as follows:

- `InvalidHeaderException`
  - bad magic marker
  - unsupported version
  - reserved/invalid flags
  - unknown/invalid header schema
  - metadata/flag consistency violations
- `InvalidPayloadException`
  - truncated stream during any read
  - declared lengths larger than remaining bytes
  - payload/integrity lengths beyond supported in-memory limits
  - trailing bytes after complete envelope decode
- `CorruptedDataException` (optional wrapper at higher layers)
  - may be used by extraction orchestration to normalize lower-level payload failures while preserving deterministic `StegoErrorCode` mapping.
