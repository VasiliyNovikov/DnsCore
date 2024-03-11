using System;

using DnsCore.Utils;

namespace DnsCore.Client;

public class DnsClientUdpOptions
{
    private const int DefaultMinSocketCount = 2;
    private static readonly int DefaultMaxSocketCount = Environment.ProcessorCount * 2;
    private const int DefaultSocketIdleTimeSeconds = 60;
    private const int DefaultSocketLifeTimeSeconds = 300;

    private int _minSocketCount = DefaultMinSocketCount;
    private int _maxSocketCount = DefaultMaxSocketCount;
    private TimeSpan _socketIdleTime = TimeSpan.FromSeconds(DefaultSocketIdleTimeSeconds);
    private TimeSpan _socketLifeTime = TimeSpan.FromSeconds(DefaultSocketLifeTimeSeconds);

    public int MinSocketCount
    {
        get => _minSocketCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSocketCount);
            _minSocketCount = value;
        }
    }

    public int MaxSocketCount
    {
        get => _maxSocketCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinSocketCount);
            _maxSocketCount = value;
        }
    }

    public TimeSpan SocketIdleTime
    {
        get => _socketIdleTime;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, SocketIdleTime);
            _socketIdleTime = value;
        }
    }

    public TimeSpan SocketLifeTime
    {
        get => _socketLifeTime;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, SocketIdleTime);
            _socketLifeTime = value;
        }
    }
}