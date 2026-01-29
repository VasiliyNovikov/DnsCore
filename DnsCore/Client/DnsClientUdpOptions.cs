using System;

namespace DnsCore.Client;

public class DnsClientUdpOptions
{
    private const int DefaultMinSocketCount = 2;
    private static readonly int DefaultMaxSocketCount = Environment.ProcessorCount * 2;
    private const int DefaultSocketIdleTimeSeconds = 60;
    private const int DefaultSocketLifeTimeSeconds = 300;

    public int MinSocketCount { get; set; } = DefaultMinSocketCount;
    public int MaxSocketCount { get; set; } = DefaultMaxSocketCount;
    public TimeSpan SocketIdleTime { get; set; } = TimeSpan.FromSeconds(DefaultSocketIdleTimeSeconds);
    public TimeSpan SocketLifeTime { get; set; } = TimeSpan.FromSeconds(DefaultSocketLifeTimeSeconds);

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(MinSocketCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxSocketCount, MinSocketCount);
        ArgumentOutOfRangeException.ThrowIfNegative(SocketIdleTime);
        ArgumentOutOfRangeException.ThrowIfLessThan(SocketLifeTime, SocketIdleTime);
    }
}