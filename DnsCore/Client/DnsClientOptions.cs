using System;

using DnsCore.Common;

namespace DnsCore.Client;

public class DnsClientOptions
{
    private const int DefaultInitialRetryDelayMilliseconds = 500;
    private const int DefaultRequestTimeoutMilliseconds = 10000;
    private const int DefaultFailureRetryCount = 3;

    public DnsTransportType TransportType { get; set; } = DnsTransportType.All;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMilliseconds(DefaultRequestTimeoutMilliseconds);
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(DefaultInitialRetryDelayMilliseconds);
    public int FailureRetryCount { get; set; } = DefaultFailureRetryCount;
    public DnsClientUdpOptions Udp { get; } = new();

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(RequestTimeout);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(InitialRetryDelay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(FailureRetryCount);
        Udp.Validate();
    }
}