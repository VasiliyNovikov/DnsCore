using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsCore.Common;

internal static class DnsSocketExtensions
{
    extension(Socket socket)
    {
        public async ValueTask SendUdpMessage(DnsTransportMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendAsync(message.Buffer.Memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }

        public async ValueTask SendUdpMessageTo(DnsTransportMessage message, EndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendToAsync(message.Buffer.Memory, SocketFlags.None, remoteEndPoint, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }

        public async ValueTask<DnsTransportMessage> ReceiveUdpMessage(CancellationToken cancellationToken)
        {
            using var buffer = new DnsTransportBuffer(DnsDefaults.MaxUdpMessageSize);
            try
            {
                var receivedBytes = await socket.ReceiveAsync(buffer.Memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                buffer.Resize((ushort)receivedBytes);
                return new DnsTransportMessage(buffer);
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }

        public async ValueTask<(EndPoint RemoteEndPoint, DnsTransportMessage Message)> ReceiveUdpMessageFrom(EndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            using var buffer = new DnsTransportBuffer(DnsDefaults.MaxUdpMessageSize);
            try
            {
                var result = await socket.ReceiveFromAsync(buffer.Memory, SocketFlags.None, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                buffer.Resize((ushort)result.ReceivedBytes);
                return (result.RemoteEndPoint, new DnsTransportMessage(buffer));
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }

        private async ValueTask SendExactly(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
                try
                {
                    while (!buffer.IsEmpty)
                        buffer = buffer[await socket.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false)..];
                }
                catch (SocketException e)
                {
                    throw new DnsSocketException(e.Message, e);
                }
        }

        public async ValueTask SendTcpMessage(DnsTransportMessage message, CancellationToken cancellationToken)
        {
            using var lengthBuffer = new DnsTransportBuffer(2);
            var messageBuffer = message.Buffer;
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer.Span, messageBuffer.Length);
            await socket.SendExactly(lengthBuffer.Memory, cancellationToken).ConfigureAwait(false);
            await socket.SendExactly(messageBuffer.Memory, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<bool> ReceiveExactly(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var totalReceivedBytes = 0;
            while (!buffer.IsEmpty)
            {
                int receivedBytes;
                try
                {
                    receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                }
                catch (SocketException e)
                {
                    throw new DnsSocketException(e.Message, e);
                }
                if (receivedBytes == 0)
                    return totalReceivedBytes == 0 ? false : throw new DnsSocketException("Failed to receive message");
                buffer = buffer[receivedBytes..];
                totalReceivedBytes += receivedBytes;
            }
            return true;
        }

        public async ValueTask<DnsTransportMessage> ReceiveTcpMessage(CancellationToken cancellationToken)
        {
            using var lengthBuffer = new DnsTransportBuffer(2);
            using var requestBuffer = new DnsTransportBuffer();
            if (!await socket.ReceiveExactly(lengthBuffer.Memory, cancellationToken).ConfigureAwait(false))
                throw new DnsSocketException("Failed to receive message length");

            var length = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer.Span);
            if (length == 0)
                throw new DnsSocketException("Received message length is zero");

            requestBuffer.Resize(length);
            return await socket.ReceiveExactly(requestBuffer.Memory, cancellationToken).ConfigureAwait(false)
                ? new DnsTransportMessage(requestBuffer)
                : throw new DnsSocketException("Failed to receive message");
        }

        public async ValueTask<Socket> AcceptTcpSocket(CancellationToken cancellationToken)
        {
            try
            {
                return await socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }

        public async ValueTask ConnectDns(EndPoint endPoint, CancellationToken cancellationToken)
        {
            try
            {
                await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw new DnsSocketException(e.Message, e);
            }
        }
    }
}