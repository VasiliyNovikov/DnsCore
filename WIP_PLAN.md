# Typed DNS Record Extraction Plan

## Snapshot

- WIP branch: `wip/typed-dns-records`
- Initial snapshot commit: `b24958d` (`WIP typed DNS record support`)
- Base commit: `8feacf3` (`master` when the snapshot was created)

This branch preserves the complete implementation as a reference. Production changes should be extracted from it into smaller topic branches rather than merged directly.

## Workflow

1. Create each topic branch from the latest accepted `master`.
2. Extract only the files and tests needed for that topic.
3. Keep every topic independently buildable and reviewable.
4. Run focused tests first, then `dotnet build`, then `dotnet test -m:1`.
5. Update documentation and dependencies only when required by that topic.
6. Merge topics in dependency order; do not merge this WIP branch.

## Extraction Order

### 1. Encoding Safeguards

- Enforce the DNS compression pointer offset limit (`0x3FFF`).
- Bound logical DNS messages to 65,535 bytes.
- Add record encoder validation hooks needed by typed codecs.
- Add focused boundary and malformed-data tests.

This is foundational and should not add new public record types.

### 2. Generic Class-Aware Encoder Registry

- Store an encoder registration per record type.
- Allow each registration to optionally declare supported DNS classes.
- Treat registrations without a class list as class-independent.
- Fall back to `DnsRawRecord` when a class-restricted encoder does not support the received class.
- Cover unrestricted lookup, restricted lookup, and raw fallback.

PX will later register only for `DnsClass.IN`.

### 3. Simple Name-Bearing Records

- Add typed RP support.
- Add typed AFSDB support.
- Add typed RT support.
- Add typed DNAME support.
- Emit embedded RDATA names uncompressed.
- Accept applicable historical compression while decoding.

These records share name-handling behavior but retain separate public models and codecs.

### 4. PX

- Add `DnsMailMappingRecord` for the RFC 2163 IN-class format.
- Register the PX codec only for `DnsClass.IN`.
- Preserve non-IN PX as opaque `DnsRawRecord` data.
- Keep raw IN PX rejected because its RDATA may contain message-relative pointers.

### 5. NAPTR

- Add typed order, preference, flags, services, regular-expression, and replacement fields.
- Encode each text field as one length-prefixed DNS character string.
- Enforce strict UTF-8 and the 255-byte limit per string.
- Emit replacement uncompressed and accept historical compression while decoding.
- Do not execute DDDS rules or regular expressions.

### 6. NXT

- Add the next-domain name and opaque legacy bitmap representation.
- Keep the bitmap distinct from modern NSEC bitmap encoding.
- Emit the name uncompressed and accept historical compression while decoding.
- Do not implement denial-of-existence validation.

### 7. SIG And KEY

- Add typed RFC 2535 fixed fields.
- Preserve algorithm-specific signature and key bytes.
- Parse algorithm-253 private identifiers as DNS names.
- Accept historical compression for algorithm-253 identifiers while decoding.
- Always emit algorithm-253 identifiers uncompressed.
- Preserve the explicit legacy no-key form without treating these records as modern DNSSEC.
- Do not implement signing, verification, or trust decisions.

### 8. Documentation And Version

- Update `README.md` with supported records and wire-only limitations.
- Update `AGENTS.md` architecture notes.
- Update record maturity and obsolescence comments.
- Set the next appropriate package minor version once the public API topics are ready to publish.
- Confirm that `Directory.Packages.props` and `requirements.txt` need no changes.

## Scope Boundaries

The extracted work provides typed wire encoding and decoding only. It does not include:

- DNAME substitution or synthesized CNAME responses
- NAPTR/DDDS rule execution
- Record-specific additional-section processing
- SIG/KEY cryptography
- NXT denial-of-existence validation
- Modern DNSSEC records
- Arbitrary-octet DNS label support
- General compression pointer-chain redesign

## Acceptance Gates

For each topic:

```bash
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsRecordEncodingTests"
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsEncodingTests"
dotnet build
dotnet test -m:1
```

Tests that bind DNS ports must remain sequential. If .NET 8 or .NET 9 runtimes are unavailable locally, all targets must still compile and the missing runtime coverage must be reported.

Every topic requires diff review, documentation/dependency assessment, and preservation of unrelated worktree changes before it is presented for merge.
