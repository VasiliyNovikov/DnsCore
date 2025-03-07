using DnsCore.Common;

namespace DnsCore.Server.Transport;

internal sealed class DnsServerTransportException(string message, DnsSocketException innerException) : DnsException(message, innerException)
{
    public bool IsTransient => innerException.IsTransient;
}