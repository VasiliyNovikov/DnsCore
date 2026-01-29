using System.Net;
using System.Net.Sockets;

namespace DnsCore.Client.Resolver;

internal sealed class TcpSocketPool(EndPoint endPoint) : SocketPool(ProtocolType.Tcp, endPoint);