using System;
using System.Linq;
using System.Net;

using DnsCore.Common;

namespace DnsCore.Server;

public class DnsServerOptions
{
    private const int DefaultAcceptRetryTimeoutMilliseconds = 10000;
    private const int DefaultAcceptRetryInitialIntervalMilliseconds = 10;
    private const int DefaultAcceptRetryMaxIntervalMilliseconds = DefaultAcceptRetryTimeoutMilliseconds / 2;

    public DnsTransportType TransportType { get; set; } = DnsTransportType.All;
    public EndPoint[] EndPoints { get; }
    public TimeSpan AcceptRetryTimeout { get; set; } = TimeSpan.FromMilliseconds(DefaultAcceptRetryTimeoutMilliseconds);
    public TimeSpan AcceptRetryInitialInterval { get; set; } = TimeSpan.FromMilliseconds(DefaultAcceptRetryInitialIntervalMilliseconds);
    public TimeSpan AcceptRetryMaxInterval { get; set; } = TimeSpan.FromMilliseconds(DefaultAcceptRetryMaxIntervalMilliseconds);

    public DnsServerOptions(params EndPoint[] endPoints)
    {
        ArgumentNullException.ThrowIfNull(endPoints);
        ArgumentOutOfRangeException.ThrowIfZero(endPoints.Length);
        EndPoints = endPoints;
    }

    public DnsServerOptions(IPAddress address, ushort port = DnsDefaults.Port)
        : this(new IPEndPoint(address, port))
    {
    }

    public DnsServerOptions(IPAddress[] addresses, ushort port = DnsDefaults.Port)
        : this([.. addresses.Select(a => new IPEndPoint(a, port))])
    {
    }

    public DnsServerOptions(ushort port = DnsDefaults.Port)
        : this([new IPEndPoint(IPAddress.Any, port), new IPEndPoint(IPAddress.IPv6Any, port)])
    {
    }

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EndPoints.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AcceptRetryTimeout);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(AcceptRetryInitialInterval);
        ArgumentOutOfRangeException.ThrowIfLessThan(AcceptRetryMaxInterval, AcceptRetryInitialInterval);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(AcceptRetryMaxInterval, AcceptRetryTimeout);
    }
}