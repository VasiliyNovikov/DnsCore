using System;
using System.Buffers;

namespace DnsCore.Common;

internal struct DnsTransportBuffer(ushort length) : IDisposable
{
    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

    private byte[]? _buffer = ArrayPool.Rent(length);
    private ushort _length = length;

    public readonly ushort Length => _length;
    public readonly Span<byte> Span => _buffer.AsSpan(0, _length);
    public readonly Memory<byte> Memory => _buffer.AsMemory(0, _length);

    public void Dispose()
    {
        if (_buffer is null)
            return;
        ArrayPool.Return(_buffer);
        _buffer = null;
    }
    
    public DnsTransportBuffer Move()
    {
        var buffer = _buffer;
        _buffer = null;
        return new DnsTransportBuffer { _buffer = buffer, _length = _length };
    }

    public void Resize(ushort length)
    {
        if (_buffer is null)
            _buffer = ArrayPool.Rent(length);
        else if (_buffer.Length < length)
        {
            ArrayPool.Return(_buffer);
            _buffer = ArrayPool.Rent(length);
        }
        _length = length;
    }
}