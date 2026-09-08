# Typed DNS Record Extraction Plan

## Snapshot

- WIP branch: `wip/typed-dns-records`
- Initial snapshot commit: `b24958d` (`WIP typed DNS record support`)
- Original snapshot base: `8feacf3` (`master` when the snapshot was created)
- Rebased implementation reference: `736cd30` (`Add typed DNS record support`)
- Verified extraction baseline: `6cf38b5` (`master`, including DNS message-size limits and the class-aware encoder registry)

This branch preserves the complete implementation as a reference. Production changes should be extracted from it into smaller topic branches rather than merged directly.

## Workflow

1. Create each topic branch from the latest accepted `master`.
2. Extract only the changes and tests needed for that topic; omit already-merged behavior and unrelated newline-only differences.
3. Keep every topic independently buildable and reviewable.
4. Run focused tests first, then `dotnet build`, then `dotnet test -m:1`.
5. Update documentation and dependencies only when required by that topic.
6. Merge topics in dependency order; do not merge this WIP branch.

## Extraction Order

### 1. Encoding Safeguards

- Add a defensive compression-offset check against `0x3FFF` before emitting a pointer.
- Add record encoder validation hooks needed by typed codecs.
- Add focused compression-boundary and typed-codec validation tests.

This is foundational and should not add new public record types.

Preserve master's existing message-size and large-response coverage; do not extract duplicate size-limit checks or tests from the WIP reference. Add exact `0x3FFF`/`0x4000` compression-boundary checks using records already on master, without depending on KEY, which is extracted later.

### 2. Simple Name-Bearing Records

- Add typed RP support.
- Add typed AFSDB support.
- Add typed RT support.
- Emit embedded RDATA names uncompressed.
- Accept applicable historical compression while decoding.

These records share name-handling behavior but retain separate public models and codecs.

### 3. PX

- Add `DnsMailMappingRecord` for the RFC 2163 IN-class format.
- Register the PX codec only for `DnsClass.IN`.
- Preserve non-IN PX as opaque `DnsRawRecord` data.
- Keep raw IN PX rejected because its RDATA may contain message-relative pointers.

### 4. NAPTR

- Add typed order, preference, flags, services, regular-expression, and replacement fields.
- Encode each text field as one length-prefixed DNS character string.
- Enforce strict UTF-8 and the 255-byte limit per string.
- Emit replacement uncompressed and accept historical compression while decoding.
- Do not execute DDDS rules or regular expressions.

### 5. NXT

- Add the next-domain name and opaque legacy bitmap representation.
- Keep the bitmap distinct from modern NSEC bitmap encoding.
- Emit the name uncompressed and accept historical compression while decoding.
- Do not implement denial-of-existence validation.

### 6. SIG And KEY

- Add typed RFC 2535 fixed fields.
- Preserve algorithm-specific signature and key bytes.
- Parse algorithm-253 private identifiers as DNS names.
- Accept historical compression for algorithm-253 identifiers while decoding.
- Always emit algorithm-253 identifiers uncompressed.
- Preserve the explicit legacy no-key form without treating these records as modern DNSSEC.
- Do not implement signing, verification, or trust decisions.

### 7. Documentation And Version

- Update `README.md` with supported records and wire-only limitations while preserving master's concise structure and getting-started links.
- Update `AGENTS.md` architecture notes.
- Update record maturity and obsolescence comments.
- Reassess the package version when the public API topics are ready to publish: master is at `0.3.0`, while the WIP reference uses `0.4.0`.
- Recheck `Directory.Packages.props` and `DnsCore.Tests/requirements.txt`; neither differs from the verified baseline, and no dependency changes are currently required.

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

The class-aware encoder registry is already on master; do not re-extract it. Generic unrestricted lookup, restricted lookup, and raw-fallback test coverage remains outstanding and should be added independently of later typed records. Extract PX-specific fallback tests with PX.

For each topic:

```bash
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsRecordEncodingTests"
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsEncodingTests"
dotnet build
dotnet test -m:1
```

Tests that bind DNS ports must remain sequential. If .NET 8 or .NET 9 runtimes are unavailable locally, all targets must still compile; run `dotnet test -m:1 --framework net10.0` instead of the final multi-target test command and report the missing runtime coverage.

Every topic requires diff review, documentation/dependency assessment, and preservation of unrelated worktree changes before it is presented for merge.
