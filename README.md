# DNS Core
.NET lightweight DNS client and server

[![DnsCore release](https://img.shields.io/nuget/v/DnsCore)](https://www.nuget.org/packages/DnsCore/)
[![DnsCore download count](https://img.shields.io/nuget/dt/DnsCore)](https://www.nuget.org/packages/DnsCore/)

## Features

- **DNS Client** — resolve DNS queries with automatic UDP→TCP fallback, retry with exponential backoff, and configurable timeouts
- **DNS Server** — handle incoming DNS requests via `IDnsServerHandler` interface or a simple delegate
- **UDP & TCP** — full support for both transport protocols
- **Hosting integration** — `AddDns()` extensions for `Microsoft.Extensions.Hosting`
- **Typed records** — A, AAAA, NS, MD, MF, CNAME, DNAME, SOA, MB, MG, MR, PTR, MINFO, MX, TXT, RP, AFSDB, RT, SIG, KEY, PX, NXT, SRV, and NAPTR
- **Targets** — net8.0, net9.0, net10.0
- **Platforms** — Linux x64, Linux arm64, Windows x64, Windows arm64, macOS arm64

Unknown record types are exposed as `DnsRawRecord` so their RDATA remains byte-transparent. Record types that can legally or historically contain message-relative DNS compression pointers require type-specific encoding and cannot be represented as raw data. PX has a defined typed format only for class IN; other classes remain byte-transparent raw records.

Typed support covers safe wire encoding and decoding only. It does not implement automatic DNAME subtree substitution, synthesized CNAME generation, resolver following, NAPTR/DDDS rule execution, record-specific additional-section processing, SIG/KEY cryptography, or NXT denial-of-existence validation. RP, AFSDB, and RT are experimental formats; AFSDB subtype 1 is deprecated in favor of SRV. PX is a legacy MIXER/X.400 format, SIG and KEY are legacy security formats retained for SIG(0)/TKEY contexts, and NXT is obsolete. Algorithm 253 identifiers in SIG and KEY accept historical DNS compression when decoding but are always emitted uncompressed. `DnsName` currently supports the library's restricted ASCII label syntax, so arbitrary DNS label octets such as `/` are unsupported; compressed pointer-to-pointer name chains are not accepted.

## DNS labels

Labels support ASCII letters, digits, underscores, asterisks (`*`), and interior hyphens, up to 63 bytes. Asterisks are preserved literally anywhere in a label, including `*.example.com` and `a*b.example.com`. This supports parsing and wire encoding/decoding of wildcard names; it does not add wildcard matching or automatic wildcard answers to the server.
