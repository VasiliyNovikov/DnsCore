using System.Buffers;

namespace DnsCore.Common;

internal static class DnsBufferPool
{
    public static byte[] Rent(ushort minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);

    public static void Return(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);

    public static void Resize(ref byte[] buffer, ushort minimumLength)
    {
        if (buffer.Length < minimumLength)
        {
            Return(buffer);
            buffer = Rent(minimumLength);
        }
    }
}