using System;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;

namespace DnsCore.Server;

internal class DnsServerDelegatingHandler(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler) : IDnsServerHandler
{
    public async ValueTask<DnsResponse> Handle(DnsRequest request, CancellationToken cancellationToken) => await handler(request, cancellationToken).ConfigureAwait(false);
}