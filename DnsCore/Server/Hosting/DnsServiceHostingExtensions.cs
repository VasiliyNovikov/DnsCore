using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;
using DnsCore.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsCore.Server.Hosting;

public static class DnsServiceHostingExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDns(EndPoint[] endPoints, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(endPoints, transportType, handler, logger));
        }

        public IServiceCollection AddDns(EndPoint endPoint, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(endPoint, transportType, handler, logger));
        }

        public IServiceCollection AddDns(IPAddress address, ushort port, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(address, port, transportType, handler, logger));
        }

        public IServiceCollection AddDns(IPAddress address, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(address, transportType, handler, logger));
        }

        public IServiceCollection AddDns(ushort port, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(port, transportType, handler, logger));
        }

        public IServiceCollection AddDns(DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(transportType, handler, logger));
        }

        public IServiceCollection AddDns(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler)
        {
            return services.AddDns(logger => new(handler, logger));
        }

        private IServiceCollection AddDns(Func<ILogger?, DnsServer> factory)
        {
            return services.AddSingleton<DnsServer>(svc => factory(svc.GetService<ILogger<DnsServer>>()))
                           .AddHostedService<DnsService>();
        }
    }
}