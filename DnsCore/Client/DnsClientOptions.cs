using System;

namespace DnsCore.Client;

public class DnsClientOptions
{
    private const int DefaultInitialRetryDelayMilliseconds = 500;
    private const int DefaultRequestTimeoutMilliseconds = 10000;
    private const int DefaultFailureRetryCount = 3;

    public TimeSpan RequestTimeout
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(DefaultRequestTimeoutMilliseconds);

    public TimeSpan InitialRetryDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = TimeSpan.FromMilliseconds(DefaultInitialRetryDelayMilliseconds);

    public int FailureRetryCount
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = DefaultFailureRetryCount;

    public DnsClientUdpOptions Udp { get; } = new();
}