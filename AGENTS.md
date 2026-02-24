# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Build, Test, and Lint

```bash
# Build
dotnet build

# Run all tests (sequential — tests bind to DNS ports)
dotnet test -m:1

# Run a single test
dotnet test -m:1 --filter "FullyQualifiedName~DnsServerTests.ResolveA_Udp"

# Run tests in a specific class
dotnet test -m:1 --filter "ClassName~DnsServerTests"
```

Warnings are treated as errors (`TreatWarningsAsErrors`). Code style is enforced at build time (`EnforceCodeStyleInBuild`). Analysis mode is `Recommended`. See `.editorconfig` for specific diagnostic suppressions.

## Architecture

DnsCore is a lightweight .NET DNS client and server library targeting net8.0, net9.0, and net10.0.

**Projects:**
- `DnsCore` — The library: client, server, model, transport, and encoding
- `DnsCore.Tests` — MSTest integration tests (uses Python via CSnakes for test DNS server)
- `DnsCore.TestServer` / `DnsCore.TestClient` — Console apps for manual testing

**Key layers inside `DnsCore/`:**
- `Model/` — DNS domain types: `DnsRequest`, `DnsResponse` (both sealed, inherit `DnsMessage`), `DnsRecord<T>` hierarchy, `DnsName` (immutable linked list), `DnsQuestion`
- `Client/` — `DnsClient` (sealed) with UDP→TCP fallback, retry with exponential backoff + jitter. `Resolver/` has transport-specific implementations (`DnsUdpResolver`, `DnsTcpResolver`)
- `Server/` — `DnsServer` (sealed) accepts `IDnsServerHandler` or a `Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>>` delegate. `Hosting/` provides `AddDns()` extensions for `Microsoft.Extensions.Hosting` integration
- `Common/` — Shared transport abstractions (`DnsTransportType`, `DnsTransportBuffer`, socket extensions)
- `IO/` — Binary `DnsReader`/`DnsWriter` for wire-format encoding
- `Model/Encoding/` — Message serialization (`DnsRequestEncoder`, `DnsResponseEncoder`)

**Request-reply pattern:** Responses are created via `request.Reply(answers)` or `request.Reply(DnsResponseStatus.NameError)` — this ensures ID and question correlation per RFC.

## Code Conventions

- All public domain types are prefixed with `Dns` (e.g., `DnsClient`, `DnsRecord`, `DnsName`)
- Private fields use underscore prefix (`_handler`, `_options`)
- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`)
- Nullable reference types are enabled globally
- `sealed` on concrete model and service classes (`DnsClient`, `DnsServer`, `DnsRequest`, `DnsResponse`, `DnsName`)
- `ValueTask` preferred over `Task` for async hot paths
- `.ConfigureAwait(false)` on all awaits in library code (CA2007 is error-severity outside tests)
- Logging uses `[LoggerMessage]` source-generated partial methods with structured parameters
- Central package version management via `Directory.Packages.props` — version numbers go there, not in individual `.csproj` files

## Test Conventions

- Framework: MSTest (`[TestClass]`, `[TestMethod]`, `[DataRow]`)
- Tests do **not** run in parallel (`[DoNotParallelize]` assembly attribute) due to DNS port conflicts
- Server tests invoke platform-specific external tools (`Resolve-DnsName` on Windows, `dig` on Unix) against an in-process `DnsServer`
- Client tests run against a Python DNS server (via CSnakes runtime) — see `test_server.py` and `requirements.txt`
- Custom assertion helper: `DnsAssert.AreEqual()` for record-level field comparison
- Transport variants (UDP/TCP) tested via `[DataRow(DnsTransportType.Udp)]` / `[DataRow(DnsTransportType.Tcp)]` parameterization