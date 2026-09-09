# Typed DNS Record Extraction Plan

## Snapshot

- WIP branch: `wip/typed-dns-records`
- Initial snapshot commit: `b24958d` (`WIP typed DNS record support`)
- Original snapshot base: `8feacf3` (`master` when the snapshot was created)
- Rebased implementation reference: `e91f963` (`Add typed DNS record support`)
- Verified extraction baseline: `a46b38f` (`master`, including DNS message-size limits, the class-aware encoder registry, and PX support)

This branch preserves the complete implementation as a reference. Production changes should be extracted from it into smaller topic branches rather than merged directly.

## Workflow

1. Create each topic branch from the latest accepted `master`.
2. Extract only the changes and tests needed for that topic; omit already-merged behavior and unrelated newline-only differences.
3. Keep every topic independently buildable and reviewable.
4. Run focused tests first, then `dotnet build`, then `dotnet test -m:1`.
5. Update documentation and dependencies only when required by that topic.
6. Merge topics in dependency order; do not merge this WIP branch.

## Extraction Order

### 1. Compression Boundary Tests

- Add exact `0x3FFF`/`0x4000` compression-target boundary tests using records already on master, without depending on KEY, which is extracted later.

This is independent test coverage and should not add new public record types.

Preserve master's existing message-size and large-response coverage. Do not extract duplicate size-limit checks, buffer clipping, or large-message tests from the WIP reference. Master already restricts cached compression targets to `0x3FFF`; omit the second offset-limit check at pointer emission.

Do not add a separate record-level pre-write validation pass or tests promising untouched output on failure. Callers must discard failed encoding output. Preserve master's PX checks inside its data codec and retain codec-local encode/decode validation needed for malformed data and default structs.

### 2. Simple Name-Bearing Records

- Add typed RP support.
- Add typed AFSDB support.
- Add typed RT support.
- Emit embedded RDATA names uncompressed.
- Accept applicable historical compression while decoding.

These records share name-handling behavior but retain separate public models and codecs.

### 3. NAPTR

- Add typed order, preference, flags, services, regular-expression, and replacement fields.
- Encode each text field as one length-prefixed DNS character string.
- Enforce strict UTF-8 and the 255-byte limit per string.
- Emit replacement uncompressed and accept historical compression while decoding.
- Do not execute DDDS rules or regular expressions.

### 4. NXT

- Add the next-domain name and opaque legacy bitmap representation.
- Keep the bitmap distinct from modern NSEC bitmap encoding.
- Emit the name uncompressed and accept historical compression while decoding.
- Do not implement denial-of-existence validation.

### 5. SIG And KEY

- Add typed RFC 2535 fixed fields.
- Preserve algorithm-specific signature and key bytes.
- Parse algorithm-253 private identifiers as DNS names.
- Accept historical compression for algorithm-253 identifiers while decoding.
- Always emit algorithm-253 identifiers uncompressed.
- Preserve the explicit legacy no-key form without treating these records as modern DNSSEC.
- Do not implement signing, verification, or trust decisions.

### 6. Documentation And Version

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

The class-aware encoder registry and PX implementation are already on master; do not re-extract them. Generic unrestricted lookup, restricted lookup, and raw-fallback test coverage remains outstanding and should be added independently of later typed records. Extract the remaining PX regression tests separately: non-IN opaque fallback, typed non-IN rejection, uncompressed wire output, and historical-compression decoding.

For each topic:

```bash
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsRecordEncodingTests"
dotnet test -m:1 --framework net10.0 --filter "ClassName~DnsEncodingTests"
dotnet build
dotnet test -m:1
```

Tests that bind DNS ports must remain sequential. If .NET 8 or .NET 9 runtimes are unavailable locally, all targets must still compile; run `dotnet test -m:1 --framework net10.0` instead of the final multi-target test command and report the missing runtime coverage.

Every topic requires diff review, documentation/dependency assessment, and preservation of unrelated worktree changes before it is presented for merge.
