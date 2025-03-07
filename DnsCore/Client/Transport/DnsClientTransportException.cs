using DnsCore.Common;

namespace DnsCore.Client.Transport;

internal sealed class DnsClientTransportException(string message, DnsSocketException? innerException = null) : DnsClientException(message, innerException);