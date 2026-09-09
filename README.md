# DNS Core
A lightweight DNS client and server for .NET 8, 9, and 10.

[![DnsCore release](https://img.shields.io/nuget/v/DnsCore)](https://www.nuget.org/packages/DnsCore/)
[![DnsCore download count](https://img.shields.io/nuget/dt/DnsCore)](https://www.nuget.org/packages/DnsCore/)

## Features

- **DNS Client** — resolve DNS queries with automatic UDP→TCP fallback, retry with exponential backoff, and configurable timeouts
- **DNS Server** — handle incoming DNS requests via `IDnsServerHandler` interface or a simple delegate
- **UDP & TCP** — full support for both transport protocols
- **Hosting integration** — `AddDns()` extensions for `Microsoft.Extensions.Hosting`
- **Typed records** — A, AAAA, NS, MD, MF, CNAME, DNAME, SOA, MB, MG, MR, PTR, MINFO, MX, TXT, RP, SRV, and PX (IN class)
- **Targets** — net8.0, net9.0, net10.0
- **Platforms** — Linux x64, Linux arm64, Windows x64, Windows arm64, macOS arm64

## Getting started

```bash
dotnet add package DnsCore
```

See the [client example](DnsCore.TestClient/Program.cs) for querying DNS servers and the [server example](DnsCore.TestServer/Program.cs) for handling requests with `AddDns()`.

## Limitations

- **Names:** ASCII only, with dots separating labels. Spaces, backslashes, and escape sequences are unsupported; convert internationalized domains to ASCII (IDNA) before parsing. DNS-SD instance names with spaces or Unicode and SOA/RP mailbox labels containing dots are unsupported. Use `ParseHostName` or `IsHostName` when stricter hostname validation is needed.
- **Raw records:** Unknown types use `DnsRawRecord`, except unsupported formats that may contain DNS compression pointers (AFSDB, RT, SIG, KEY, NXT, NAPTR), which are rejected. Raw RP and IN-class PX are also rejected; non-IN PX is preserved as opaque data.
- **Name compression:** Encoding follows each record field’s compression rules. Decoding accepts historical compressed names where applicable, while retaining malformed-name and compression-pointer validation.
- **RP:** Mailbox and TXT-record name in all DNS classes; root (`DnsName.Empty`) means no mailbox or no associated TXT data. Wire encoding and decoding only, without automatic TXT lookup. Embedded names are emitted uncompressed.
- **PX:** RFC 2163 IN-class wire format only; no RFC 822/X.400 conversion or mail-routing behavior. Embedded names are emitted uncompressed.
- **DNAME:** Encoding and decoding only; no automatic subtree substitution, synthesized CNAMEs, or resolver following.
- **Wildcards:** Asterisks are literal; wildcard matching and automatic wildcard answers are not provided.
