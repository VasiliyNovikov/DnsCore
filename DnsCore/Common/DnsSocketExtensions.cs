using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Common;

internal static class DnsSocketExtensions
{
    public static async ValueTask SendUdpMessageTo(this Socket socket, DnsTransportMessage message, EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendToAsync(message.Buffer, SocketFlags.None, remoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException e)
        {
            throw new DnsSocketException(e.Message, e);
        }
    }

    public static async ValueTask<(EndPoint RemoteEndPoint, DnsTransportMessage Message)> ReceiveUdpMessageFrom(this Socket socket, EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var buffer = DnsBufferPool.Rent(DnsDefaults.MaxUdpMessageSize);
        try
        {
            var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken).ConfigureAwait(false);
            return (result.RemoteEndPoint, new DnsTransportMessage(buffer, result.ReceivedBytes));
        }
        catch (SocketException e)
        {
            DnsBufferPool.Return(buffer);
            throw new DnsSocketException(e.Message, e);
        }
    }

    public static async ValueTask SendTcpMessage(this Socket socket, DnsTransportMessage message, CancellationToken cancellationToken)
    {
        var lengthBuffer = DnsBufferPool.Rent(2);
        try
        {
            var lengthBufferMem = lengthBuffer.AsMemory(0, 2);
            var buffer = message.Buffer;
            BinaryPrimitives.WriteUInt16BigEndian(lengthBufferMem.Span, (ushort)buffer.Length);
            await socket.SendAsync(lengthBufferMem, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            while (!buffer.IsEmpty)
            {
                var sentBytes = await socket.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                buffer = buffer[sentBytes..];
            }
        }
        catch (SocketException e)
        {
            throw new DnsSocketException(e.Message, e);
        }
        finally
        {
            DnsBufferPool.Return(lengthBuffer);
        }
    }

    public static async ValueTask<DnsTransportMessage?> ReceiveTcpMessage(this Socket socket, CancellationToken cancellationToken)
    {
        var lengthBuffer = DnsBufferPool.Rent(2);
        byte[]? requestBuffer = null;
        try
        {
            var lengthBufferMem = lengthBuffer.AsMemory(0, 2);
            var receivedBytes = await socket.ReceiveAsync(lengthBufferMem, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            if (receivedBytes == 0)
                return null;

            var length = BinaryPrimitives.ReadUInt16BigEndian(lengthBufferMem.Span);
            if (length == 0)
                throw new DnsSocketException("Received message length is zero");

            requestBuffer = DnsBufferPool.Rent(length);
            var totalReceivedBytes = 0;
            while (totalReceivedBytes < length)
            {
                receivedBytes = await socket.ReceiveAsync(requestBuffer.AsMemory(totalReceivedBytes, length - totalReceivedBytes), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (receivedBytes == 0)
                {
                    DnsBufferPool.Return(requestBuffer);
                    throw new DnsSocketException("Failed to receive message");
                }

                totalReceivedBytes += receivedBytes;
            }
            return new DnsTransportMessage(requestBuffer, totalReceivedBytes);
        }
        catch (SocketException e)
        {
            if (requestBuffer is not null)
                DnsBufferPool.Return(requestBuffer);
            throw new DnsSocketException(e.Message, e);
        }
        finally
        {
            DnsBufferPool.Return(lengthBuffer);
        }
    }
}