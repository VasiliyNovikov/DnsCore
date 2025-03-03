using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;

namespace DnsCore.Server.Transport.Tcp;

internal sealed class DnsTcpServerTransportConnection : DnsServerTransportConnection
{
    private readonly Socket _socket;

    public override DnsTransportType TransportType => DnsTransportType.TCP;
    public override ushort DefaultMessageSize => DnsDefaults.DefaultTcpMessageSize;
    public override ushort MaxMessageSize => DnsDefaults.MaxTcpMessageSize;
    public override EndPoint RemoteEndPoint => _socket.RemoteEndPoint!;

    internal DnsTcpServerTransportConnection(Socket socket) => _socket = socket;

    public override void Dispose() => _socket.Dispose();

    public override async ValueTask<DnsTransportMessage?> Receive(CancellationToken cancellationToken)
    {
        var lengthBuffer = DnsBufferPool.Rent(2);
        try
        {
            var lengthBufferMem = lengthBuffer.AsMemory(0, 2);
            var receivedBytes = await _socket.ReceiveAsync(lengthBufferMem, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            if (receivedBytes == 0)
                return null;

            var length = BinaryPrimitives.ReadUInt16BigEndian(lengthBufferMem.Span);
            if (length == 0)
                return null;

            var requestBuffer = DnsBufferPool.Rent(length);
            var totalReceivedBytes = 0;
            while (totalReceivedBytes < length)
            {
                receivedBytes = await _socket.ReceiveAsync(requestBuffer.AsMemory(totalReceivedBytes, length - totalReceivedBytes), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (receivedBytes == 0)
                    return null;

                totalReceivedBytes += receivedBytes;
            }
            return new DnsTransportMessage(requestBuffer, totalReceivedBytes);
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to receive request", e);
        }
        finally
        {
            DnsBufferPool.Return(lengthBuffer);
        }
    }

    public override async ValueTask Send(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var lengthBuffer = DnsBufferPool.Rent(2);
        try
        {
            var lengthBufferMem = lengthBuffer.AsMemory(0, 2);
            BinaryPrimitives.WriteUInt16BigEndian(lengthBufferMem.Span, (ushort)buffer.Length);
            await _socket.SendAsync(lengthBufferMem, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            while (!buffer.IsEmpty)
            {
                var sentBytes = await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                buffer = buffer[sentBytes..];
            }
        }
        catch (SocketException e)
        {
            throw new DnsServerTransportException("Failed to send response", e);
        }
        finally
        {
            DnsBufferPool.Return(lengthBuffer);
        }
    }
}