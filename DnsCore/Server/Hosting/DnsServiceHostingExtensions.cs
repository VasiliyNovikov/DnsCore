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
        public IServiceCollection AddDns(DnsServerOptions? options = null)
        {
            return services.AddHostedService<DnsService>(svc => new(svc.GetRequiredService<IDnsServerHandler>(), options, svc.GetRequiredService<ILogger<DnsService>>()));
        }

        public IServiceCollection AddDns<THandler>(DnsServerOptions? options = null)
            where THandler : class, IDnsServerHandler
        {
            return services.AddSingleton<IDnsServerHandler, THandler>()
                           .AddDns(options);
        }

        public IServiceCollection AddDns(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, DnsServerOptions? options = null)
        {
            return services.AddSingleton<IDnsServerHandler>(_ => new DnsServerDelegatingHandler(handler))
                           .AddDns(options);
        }

    }
}