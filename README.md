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

Labels support ASCII letters, digits, underscores, asterisks (`*`), and interior hyphens, up to 63 bytes. Asterisks are preserved literally anywhere in a label, including `*.example.com` and `a*b.example.com`. This supports parsing and wire encoding/decoding of wildcard names; it does not add wildcard matching or automatic wildcard answers to the server.

## DNAME support

Typed DNAME support covers wire encoding and decoding. Automatic subtree substitution, synthesized CNAME generation, and resolver following are not implemented.

DNAME owners and targets use the same `DnsName` and `DnsLabel` representation as other records, so arbitrary DNS label octets such as `/` are not supported.
