using System;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsCore.Server.Hosting;

public static class DnsServiceHostingExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDns(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, DnsServerOptions? options = null)
        {
            return services.AddSingleton<DnsServer>(svc => new(handler, options, svc.GetService<ILogger<DnsServer>>()))
                           .AddHostedService<DnsService>();
        }
    }
}