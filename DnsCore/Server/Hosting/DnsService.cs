using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsCore.Server.Hosting;

internal class DnsService(IDnsServerHandler handler, DnsServerOptions? options, ILogger<DnsService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await new DnsServer(handler, options, logger).Run(stoppingToken).ConfigureAwait(false);
    }
}