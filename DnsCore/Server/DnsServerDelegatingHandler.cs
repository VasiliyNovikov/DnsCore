using System;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;

namespace DnsCore.Server;

internal class DnsServerDelegatingHandler(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler) : IDnsServerHandler
{
    public ValueTask<DnsResponse> Handle(DnsRequest request, CancellationToken cancellationToken) => handler(request, cancellationToken);
}