using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace DnsCore.Server.Hosting;

internal class DnsService(DnsServer server) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await server.Run(stoppingToken).ConfigureAwait(false);
}