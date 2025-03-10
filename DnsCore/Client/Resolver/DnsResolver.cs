using System;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;

namespace DnsCore.Client.Resolver;

internal abstract class DnsResolver : IAsyncDisposable
{
    public abstract ValueTask<DnsResponse> Resolve(DnsRequest request, CancellationToken cancellationToken);
    public abstract ValueTask DisposeAsync();
}