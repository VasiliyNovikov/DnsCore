# AGENTS.md

This file is the **single source of truth** for all project documentation. `CLAUDE.md` redirects here.

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

# Manual testing (console apps target net10.0 only)
dotnet run --project DnsCore.TestServer -- -p 5553
dotnet run --project DnsCore.TestClient -- -q example.com -s 127.0.0.1 -p 5553
```

Warnings are treated as errors (`TreatWarningsAsErrors`). Code style is enforced at build time (`EnforceCodeStyleInBuild`). Analysis mode is `Recommended`. `LangVersion` is `preview`. See `.editorconfig` for specific diagnostic suppressions.

## Architecture

DnsCore is a lightweight .NET DNS client and server library targeting net8.0, net9.0, and net10.0.

**Projects:**
- `DnsCore` — The library: client, server, model, transport, and encoding (`AllowUnsafeBlocks: true`)
- `DnsCore.Tests` — MSTest integration tests (uses Python via CSnakes for test DNS server)
- `DnsCore.TestServer` — Console app for manual server testing. Pre-configured with CNAME, A, AAAA, TXT records. Uses `System.CommandLine` for `-a/--address`, `-p/--port`, `-t/--transport` options. Uses `AddDns()` hosting extension
- `DnsCore.TestClient` — Console app for manual client testing. Uses `System.CommandLine` for `-s/--server`, `-p/--port`, `-t/--type`, `-q/--query` (required) options

**Key layers inside `DnsCore/`:**

- `Model/` — DNS domain types:
  - `DnsMessage` (abstract) → `DnsRequest` (sealed), `DnsResponse` (sealed)
  - `DnsRecordBase` (abstract) → `DnsRecord` (abstract) → `DnsRecord<T>` (abstract generic)
    - `DnsAddressRecord` (sealed) — A/AAAA, wraps `IPAddress`
    - `DnsNameRecord` (abstract) → `DnsCNameRecord` (sealed), `DnsPtrRecord` (sealed) — wraps `DnsName`
    - `DnsTextRecord` — TXT, wraps `string`
    - `DnsRawRecord` (sealed) — untyped `byte[]` fallback
  - `DnsName` (sealed) — immutable linked list of `DnsLabel` nodes. Supports case-insensitive equality, `ISpanFormattable`, parsing from string. Each node holds one label and a `Parent` reference
  - `DnsLabel` (readonly struct) — single DNS label (max 63 bytes), validates ASCII letters/digits/hyphens
  - `DnsQuestion` (sealed) — question record with Name, RecordType, Class; supports equality
  - Enums: `DnsRecordType` (A, NS, CNAME, PTR, TXT, AAAA, etc.), `DnsClass` (IN, CS, CH, HS, ANY), `DnsResponseStatus` (Ok, FormatError, ServerFailure, NameError, NotImplemented, Refused), `DnsRequestType` (Query, InverseQuery, Status)
- `Client/` — `DnsClient` (sealed) with UDP→TCP fallback, retry with exponential backoff + jitter. Round-robins across server endpoints via `Interlocked` index. `Resolver/` has transport-specific implementations (`DnsUdpResolver`, `DnsTcpResolver`). `SocketPool/` manages UDP socket pooling (configurable min/max sockets, idle/lifetime timeouts, background cleanup). Request IDs generated via `RandomNumberGenerator` (cryptographically secure)
  - `DnsClientOptions` — `TransportType` (default All), `RequestTimeout` (10s), `InitialRetryDelay` (500ms), `FailureRetryCount` (3)
  - `DnsClientUdpOptions` — `MinSocketCount` (2), `MaxSocketCount` (ProcessorCount×2), `SocketIdleTime` (60s), `SocketLifeTime` (300s)
  - `SystemDnsConfiguration` — static class that reads system DNS servers from network interfaces
- `Server/` — `DnsServer` (sealed) accepts `IDnsServerHandler` or a `Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>>` delegate. Handles response encoding with automatic truncation (doubles buffer up to max, then sets Truncated flag). Uses `ServerTaskScheduler` (Channel\<Task\>-based concurrent task coordinator) for request processing
  - `DnsServerOptions` — `TransportType` (default All), `EndPoints` (default: all interfaces port 53), `AcceptRetryTimeout` (10s), `AcceptRetryInitialInterval` (10ms), `AcceptRetryMaxInterval` (5s), configurable log levels for transport errors/decoding errors/truncation
  - `Hosting/` — `DnsServiceHostingExtensions` provides `AddDns()` extension methods (three overloads: pre-registered handler, generic `THandler`, delegate). `DnsService` (internal) is a `BackgroundService`
- `Common/` — Shared transport abstractions:
  - `DnsTransportType` enum: UDP, TCP, All
  - `DnsTransportBuffer` (struct) — `ArrayPool<byte>`-backed memory pooling with `Resize()` and `Move()` semantics
  - `DnsSocketExtensions` — async UDP send/receive, TCP framed send/receive (2-byte length prefix), socket accept/connect helpers. Wraps `SocketException` → `DnsSocketException`
  - `DnsDefaults` — Port=53, DefaultUdpMessageSize=256, MaxUdpMessageSize=512, DefaultTcpMessageSize=1024, MaxTcpMessageSize=65535
  - Backport polyfills for pre-net9.0: `Lock` class, `Task.WhenAny(ReadOnlySpan<Task>)`, `ArgumentOutOfRangeException` for `TimeSpan`
- `IO/` — `DnsReader`/`DnsWriter` (both `ref struct` for zero-allocation stack use). Read/write big-endian integers via `IBinaryInteger<T>`. Support DNS message compression (RFC 1035) via offset↔name dictionaries
- `Model/Encoding/` — `DnsRequestEncoder`/`DnsResponseEncoder` (public static Encode/Decode methods). Internally: `DnsRawMessageEncoder` handles header + sections, `DnsNameEncoder` handles name compression (pointer = offset | 0xC000), `DnsRecordEncoder` dispatches to type-specific data encoders (address, name, text, raw). Throws `FormatException` on any encoding/decoding error

**Exception hierarchy:**
- `DnsException` — base for all DNS exceptions
  - `DnsClientException` — general client errors
    - `DnsResponseException` — response-level errors (has `Response` property)
      - `DnsResponseStatusException` (sealed) — server returned error status
      - `DnsResponseTruncatedException` (sealed) — response was truncated
  - `DnsSocketException` — socket errors with `IsTransient` flag (transient: ConnectionReset, NetworkUnreachable, HostUnreachable, ConnectionRefused, etc.)

**Request-reply pattern:** Responses are created via `request.Reply(answers)` or `request.Reply(DnsResponseStatus.NameError)` — this ensures ID and question correlation per RFC 1035.

## CI

The GitHub Actions pipeline (`.github/workflows/pipeline.yml`) has two jobs:

- **validate** — builds and tests across a matrix of runners: `ubuntu-latest` (x64), `ubuntu-24.04-arm` (arm64), `windows-latest` (x64), `macos-latest` (arm64). Sets up .NET 8.0, 9.0, 10.0. Uploads `.trx` test results as artifacts
- **publish** — packs and pushes to NuGet (runs only on master when `PUBLISH` variable is `true` or `auto`). Version is computed by `.github/workflows/package_version.cs`: reads base version from `DnsCore.csproj`, queries NuGet for existing versions, increments patch. Non-master branches get a `-beta-{run_id}` suffix

## Code Conventions

- All public domain types are prefixed with `Dns` (e.g., `DnsClient`, `DnsRecord`, `DnsName`)
- Private fields use underscore prefix (`_handler`, `_options`)
- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`)
- Nullable reference types are enabled globally
- `sealed` on concrete model and service classes (`DnsClient`, `DnsServer`, `DnsRequest`, `DnsResponse`, `DnsName`, exception leaf classes, record leaf classes)
- `ref struct` for performance-critical stack-only types (`DnsReader`, `DnsWriter`)
- `ValueTask` preferred over `Task` for async hot paths
- `.ConfigureAwait(false)` on all awaits in library code (CA2007 is error-severity outside tests)
- Logging uses `[LoggerMessage]` source-generated partial methods with structured parameters
- Central package version management via `Directory.Packages.props` — version numbers go there, not in individual `.csproj` files. Transitive pinning is enabled (`CentralPackageTransitivePinningEnabled`)
- C# preview features are used (e.g., `extension` blocks for extension methods in hosting)
- `using` directives: system namespaces first (`dotnet_sort_system_directives_first`), separated by blank line (`dotnet_separate_import_directive_groups`)

## Test Conventions

- Framework: MSTest (`[TestClass]`, `[TestMethod]`, `[DataRow]`)
- Tests do **not** run in parallel (`[DoNotParallelize]` assembly attribute) due to DNS port conflicts
- Transport variants (UDP/TCP/All) tested via `[DataRow(DnsTransportType.Udp)]` / `[DataRow(DnsTransportType.Tcp)]` parameterization
- Custom assertion helper: `DnsAssert.AreEqual()` for record-level field comparison (type-aware: compares IPAddress for A/AAAA, DnsName for CNAME/PTR, string for TXT, byte[] for raw)

**Server tests** (`DnsServerTests`):
- Invoke platform-specific external tools against an in-process `DnsServer`
- Windows: `pwsh -Command "Resolve-DnsName ... | ConvertTo-Json"` on port 53
- Unix: `dig @addr -p port -t type +nocmd +noall +answer +nostats` on port 5553 (avoids privilege requirements)
- Test CNAME+A, AAAA, CNAME, NameError, and long TXT records

**Client tests** (`DnsClientTests`):
- Run against a Python DNS server (via CSnakes runtime) on port 12353
- Python runtime initialized in static constructor: `WithPython()` → `WithVirtualEnvironment(.venv)` → `WithUvInstaller()` → `FromRedistributable()`
- `test_server.py` uses `dnslib` (pinned at 0.9.26 in `requirements.txt`). `TestResolver` returns pre-encoded responses sequentially; `TestDNSServer` runs dual UDP+TCP threads
- Tests: resolution, truncation retry/failure, failure retry with backoff, error status, timeout

**Encoding tests** (`DnsEncodingTests`):
- Round-trip encode/decode for all record types and status codes
- Fuzz testing: 1000 random byte sequences must throw `FormatException`
- Compression pointer loop detection (self-referencing, multi-level loops, out-of-bounds pointers)

**Other tests:** `DnsServerTaskSchedulerTests` (task lifecycle, exception propagation, cancellation), `SystemDnsConfigurationTests` (system resolver readable)

## Documentation Rules

**After every change, feature, or fix:**
- Review and update `AGENTS.md` and `README.md` if the change affects documented behavior, conventions, architecture, CI, or public-facing information
- Check if NuGet (`Directory.Packages.props`) or pip (`requirements.txt`) dependencies need updating and update them

This is mandatory, not optional.
