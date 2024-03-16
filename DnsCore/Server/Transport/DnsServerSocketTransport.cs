using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsCore.Server.Transport;

internal abstract class DnsServerSocketTransport : DnsServerTransport
{
    protected Socket Socket { get; }

    protected DnsServerSocketTransport(EndPoint endPoint, SocketType socketType, ProtocolType protocolType)
    {
        Socket = new Socket(endPoint.AddressFamily, socketType, protocolType);
        Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Socket.Bind(endPoint);
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        try
        {
            Socket.Dispose();
            return ValueTask.CompletedTask;
        }
        catch (SocketException e)
        {
            return ValueTask.FromException(e);
        }
    }
}