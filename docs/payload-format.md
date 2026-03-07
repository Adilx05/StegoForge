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
| `KdfAlgorithmId` | UTF-8 string (optional) | KDF identifier when encryption is enabled (for example `pbkdf2-sha256`). Must be `none`/empty when encryption is disabled. |
| `KdfParameterMetadata` | UTF-8 string (optional) | Deterministic parameter string for KDF settings (for example iteration count or memory/cpu profile). Must be parseable without secret material. |
| `CipherAlgorithmId` | UTF-8 string | AEAD algorithm identifier (for example `aes-256-gcm`, `chacha20-poly1305`) or `none` when not encrypted. |
| `SaltMetadata` | UTF-8 string (optional) | Encoded KDF salt metadata. For v1, value is base64url-encoded raw salt bytes when present. |
| `NonceMetadata` | UTF-8 string (optional) | Encoded AEAD nonce/IV metadata. For v1, value is base64url-encoded raw nonce bytes when present. |
| `TagLengthBytes` | UInt16 | Authentication tag size in bytes. Must be `0` when encryption is disabled; otherwise must match `IntegrityLength`. |

### Crypto metadata handling rules (v1)

- `CipherAlgorithmId` is the canonical selector for crypto provider resolution during extract.
- `KdfAlgorithmId` + `KdfParameterMetadata` + `SaltMetadata` together define key-derivation expectations; no secret password bytes are ever serialized.
- `NonceMetadata` must be unique per encryption operation for a given derived key.
- AEAD authentication tag bytes are serialized in `IntegrityData`; the header carries only algorithm and sizing metadata (`TagLengthBytes`).
- Header metadata values must be ASCII/UTF-8 normalized to lowercase identifiers for deterministic comparisons.

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


## Compression flag + descriptor interpretation (finalized v1 behavior)

Compression state is encoded redundantly by design and must remain internally consistent:

- `Flags` bit 0 (`payload compressed`) is the authoritative processing signal for extraction behavior.
- `Header.CompressionDescriptor` carries the algorithm identity used at embed time (`deflate`, `brotli`, etc.) or `none` when compression is not applied.

Extraction logic interprets these fields as follows:

1. If bit 0 is **clear**, extraction must treat `Payload` as uncompressed and must not invoke a decompressor.
2. If bit 0 is **set**, extraction must invoke the decompressor path for `Payload`; malformed or truncated compressed bytes must fail deterministically as `InvalidPayloadException`.
3. Header/flag mismatches (for example, compressed flag set with descriptor `none`, or compressed flag clear with non-`none` descriptor) are header-contract violations and should be rejected as `InvalidHeaderException`.

This rule set ensures the wire format remains machine-readable while preserving deterministic error mapping across CLI and GUI surfaces.

## Versioning strategy

- Magic identifies StegoForge payloads.
- Version increments for incompatible wire changes.
- Unknown version values must return an unsupported-version error.
- New header fields may be added only via a new header schema/version marker; v1 parsers must reject unknown schema variants rather than attempting partial decode.
- Algorithm IDs are versioned independently from envelope version; unsupported algorithm IDs in a supported envelope version must fail as `UnsupportedFormat`, not unsupported envelope version.
- Backward-compatible parser behavior for v1 requires strict validation of crypto metadata combinations:
  - encrypted flag set + missing/`none` cipher id => invalid header
  - encrypted flag clear + non-empty cipher/KDF/salt/nonce/tag metadata => invalid header
  - `TagLengthBytes` mismatch with `IntegrityLength` => invalid payload/header contract violation
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


## BMP carrier limitations and compatibility expectations

For `bmp-lsb-v1`, carrier compatibility is intentionally strict to keep extraction deterministic:

- only 24-bit BGR (`BitsPerPixel=24`) and 32-bit BGRA (`BitsPerPixel=32`) BMPs are accepted,
- `BI_RGB` (`Compression=0`) is accepted for 24-bit carriers, and `BI_RGB` or `BI_BITFIELDS` (`Compression=3`) is accepted for 32-bit carriers,
- indexed/paletted BMPs, RLE/bitfield-compressed BMPs, and other bit depths are rejected before embed/extract proceeds.

Failure mapping is stable by policy:

- unsupported bit depth/compression => `UnsupportedFormatException` => `StegoErrorCode.UnsupportedFormat`,
- malformed or truncated BMP structural header => `InvalidHeaderException` => `StegoErrorCode.InvalidHeader`.

This allows callers to distinguish format-policy incompatibility from malformed carrier data while still receiving actionable messages with detected values and accepted format set.

## PNG carrier limitations and integrity expectations

For PNG-LSB carriers, payload integrity depends on preserving exact pixel-channel least-significant bits after embed. As a result:

- the carrier must remain 8-bit `Rgb` or `RgbWithAlpha`; other color models/bit depths are rejected by the handler,
- re-encoding through tooling that changes bit depth, palette/indexing, color type, or applies lossy transforms will invalidate embedded payload bits,
- ancillary metadata/chunk layout is not part of the payload contract and may change across re-saves even when pixel data remains valid.

Testing therefore validates both PNG structural decodability and post-embed extractability as separate requirements. A PNG can be structurally valid yet still contain corrupted envelope bytes, which must surface as deterministic `InvalidHeader`/`InvalidPayload` typed errors at envelope-parse time.
