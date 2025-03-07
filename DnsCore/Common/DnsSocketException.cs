using System.Collections.Frozen;
using System.Net.Sockets;

namespace DnsCore.Common;

public class DnsSocketException(string message, SocketException? innerException = null) : DnsException(message, innerException)
{
    private static readonly FrozenSet<SocketError> TransientSocketErrors = new[]
    {
        SocketError.ConnectionReset,
        SocketError.ConnectionAborted,
        SocketError.NoBufferSpaceAvailable,
        SocketError.TooManyOpenSockets,
        SocketError.NetworkDown,
        SocketError.NetworkUnreachable,
        SocketError.NetworkReset,
        SocketError.HostDown,
        SocketError.HostUnreachable,
        SocketError.HostNotFound,
        SocketError.MessageSize,
        SocketError.InProgress,
        SocketError.ConnectionRefused,
    }.ToFrozenSet();

    public bool IsTransient => innerException is not null && TransientSocketErrors.Contains(innerException.SocketErrorCode);
}