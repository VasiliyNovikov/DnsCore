# DNS Core
A lightweight DNS client and server for .NET 8, 9, and 10.

[![DnsCore release](https://img.shields.io/nuget/v/DnsCore)](https://www.nuget.org/packages/DnsCore/)
[![DnsCore download count](https://img.shields.io/nuget/dt/DnsCore)](https://www.nuget.org/packages/DnsCore/)

## Features

- **DNS Client** — resolve DNS queries with automatic UDP→TCP fallback, retry with exponential backoff, and configurable timeouts
- **DNS Server** — handle incoming DNS requests via `IDnsServerHandler` interface or a simple delegate
- **UDP & TCP** — full support for both transport protocols
- **Hosting integration** — `AddDns()` extensions for `Microsoft.Extensions.Hosting`
- **Typed records** — A, AAAA, NS, MD, MF, CNAME, DNAME, SOA, MB, MG, MR, PTR, MINFO, MX, TXT, RP, AFSDB, RT, SIG, KEY, PX (IN class), NXT, SRV, and NAPTR
- **Targets** — net8.0, net9.0, net10.0
- **Platforms** — Linux x64, Linux arm64, Windows x64, Windows arm64, macOS arm64

## Getting started

```bash
dotnet add package DnsCore
```

See the [client example](DnsCore.TestClient/Program.cs) for querying DNS servers and the [server example](DnsCore.TestServer/Program.cs) for handling requests with `AddDns()`.

## Limitations

- **Names:** ASCII only, with dots separating labels. Spaces, backslashes, and escape sequences are unsupported; convert internationalized domains to ASCII (IDNA) before parsing. DNS-SD instance names with spaces or Unicode and SOA mailbox labels containing dots are unsupported. Use `ParseHostName` or `IsHostName` when stricter hostname validation is needed.
- **Raw records:** Unknown types use byte-transparent `DnsRawRecord` data. Formats that may contain DNS compression pointers require typed records instead. Raw IN-class PX is also rejected; non-IN PX is preserved as opaque data.
- **PX:** RFC 2163 IN-class wire format only; no RFC 822/X.400 conversion or mail-routing behavior. Embedded names are emitted uncompressed; historical compressed input is accepted.
- **DNAME:** Encoding and decoding only; no automatic subtree substitution, synthesized CNAMEs, or resolver following.
- **Typed records:** Wire encoding and decoding only; no NAPTR/DDDS rule execution, record-specific additional-section processing, SIG/KEY cryptography, or NXT denial-of-existence validation. RP, AFSDB, and RT are experimental; AFSDB subtype 1 is deprecated in favor of SRV. PX is a legacy MIXER/X.400 format, SIG and KEY are legacy security formats retained for SIG(0)/TKEY contexts, and NXT is obsolete.
- **Compression:** Pointer-to-pointer name chains are not accepted. Algorithm 253 identifiers in SIG and KEY accept historical compression when decoding but are emitted uncompressed.
- **Wildcards:** Asterisks are literal; wildcard matching and automatic wildcard answers are not provided.
