# DNS Core
.NET lightweight DNS client and server

[![DnsCore release](https://img.shields.io/nuget/v/DnsCore)](https://www.nuget.org/packages/DnsCore/)
[![DnsCore download count](https://img.shields.io/nuget/dt/DnsCore)](https://www.nuget.org/packages/DnsCore/)

## Features

- **DNS Client** — resolve DNS queries with automatic UDP→TCP fallback, retry with exponential backoff, and configurable timeouts
- **DNS Server** — handle incoming DNS requests via `IDnsServerHandler` interface or a simple delegate
- **UDP & TCP** — full support for both transport protocols
- **Hosting integration** — `AddDns()` extensions for `Microsoft.Extensions.Hosting`
- **Typed records** — A, AAAA, NS, MD, MF, CNAME, SOA, MB, MG, MR, PTR, MINFO, MX, TXT, and SRV
- **Targets** — net8.0, net9.0, net10.0
- **Platforms** — Linux x64, Linux arm64, Windows x64, Windows arm64, macOS arm64

Unknown record types are exposed as `DnsRawRecord` so their RDATA remains byte-transparent. Record types that can legally or historically contain message-relative DNS compression pointers require type-specific encoding; unsupported types in that category are rejected instead of being exposed as unsafe raw data. This currently includes the legacy RP, AFSDB, RT, SIG, KEY, PX, NXT, NAPTR, and DNAME formats.
