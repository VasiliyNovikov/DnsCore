using System;
using System.Buffers;

namespace DnsCore.Server.Transport;

public sealed class DnsServerTransportRequest(byte[] buffer, int length) : IDisposable
{
    public static byte[] AllocateBuffer(int length) => ArrayPool<byte>.Shared.Rent(length);
    
    public ReadOnlySpan<byte> Buffer => buffer.AsSpan(0, length);

    public void Dispose() => ArrayPool<byte>.Shared.Return(buffer);
}