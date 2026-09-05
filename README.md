# DNS Core
.NET lightweight DNS client and server

[![DnsCore release](https://img.shields.io/nuget/v/DnsCore)](https://www.nuget.org/packages/DnsCore/)
[![DnsCore download count](https://img.shields.io/nuget/dt/DnsCore)](https://www.nuget.org/packages/DnsCore/)

## Features

- **DNS Client** — resolve DNS queries with automatic UDP→TCP fallback, retry with exponential backoff, and configurable timeouts
- **DNS Server** — handle incoming DNS requests via `IDnsServerHandler` interface or a simple delegate
- **UDP & TCP** — full support for both transport protocols
- **Hosting integration** — `AddDns()` extensions for `Microsoft.Extensions.Hosting`
- **Typed records** — A, AAAA, NS, MD, MF, CNAME, DNAME, SOA, MB, MG, MR, PTR, MINFO, MX, TXT, and SRV
- **Targets** — net8.0, net9.0, net10.0
- **Platforms** — Linux x64, Linux arm64, Windows x64, Windows arm64, macOS arm64

Unknown record types are exposed as `DnsRawRecord` so their RDATA remains byte-transparent. Record types that can legally or historically contain message-relative DNS compression pointers require type-specific encoding; unsupported types in that category are rejected instead of being exposed as unsafe raw data. This currently includes the legacy RP, AFSDB, RT, SIG, KEY, PX, NXT, and NAPTR formats.

## DNS labels

Labels support printable ASCII (`!` through `~`) except dots and backslashes, up to 63 bytes per label and 255 bytes per complete uncompressed name (including length prefixes and the root octet). All record types use these same label rules, without additional hostname restrictions during encoding or decoding. This supports classless reverse-DNS names containing `/` and SOA mailbox punctuation such as `+`.

`DnsName.Parse` always treats dots as label separators. Empty text and `.` represent the root name, but interior empty labels are rejected. Escape sequences are not supported. Spaces, control characters, backslashes, non-ASCII characters, and dots within a label are rejected in both text parsing and wire decoding. Consequently, SOA mailbox labels containing dots and DNS-SD instance labels containing spaces or Unicode are not currently supported.

`DnsLabel` stores string-backed `ReadOnlyMemory<char>` slices, reusing input strings; `Span` exposes the unchanged text and `Length` counts characters/bytes. `DnsName.Length` counts presentation characters including the trailing dot, with `DnsName.MaxLength` equal to 254. Uncompressed wire size is one byte larger for non-root names; the root occupies one byte. Formatting is not shell escaping or a zone-file serializer.

```csharp
var reverse = DnsName.Parse("129.128/26.2.0.192.in-addr.arpa.");
var host = DnsName.ParseHostName("printer.example.com");
bool isHostName = reverse.IsHostName; // false: contains '/'
```

Both labels and names offer `IsHostName` and `ParseHostName` for optional ASCII letters/digits/interior-hyphens validation. Leading digits and a trailing name dot are accepted; root names, wildcards, and underscores are not hostnames. IDNA conversion is left to callers; these APIs validate syntax, not IDNA A-labels or domain registration.

Equality and compression matching ignore ASCII letter case; hashing follows the same rules. A default `DnsLabel` equals `DnsLabel.Empty` and has the same hash code. Compression may reuse the casing of an earlier suffix. Asterisks remain literal; parsing them does not add wildcard matching or automatic wildcard answers.

Parsing and construction reject names exceeding 255 wire bytes; parsing also rejects repeated trailing dots. The constructor preserves the supplied parent. ASCII hostname presentation and `DnsName.Length` remain unchanged.

## DNAME support

Typed DNAME support covers wire encoding and decoding. Automatic subtree substitution, synthesized CNAME generation, and resolver following are not implemented.

DNAME owners and targets support the same general DNS labels as other records.
