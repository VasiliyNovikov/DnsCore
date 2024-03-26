using System;

namespace DnsCore.Client.Transport;

internal sealed class DnsClientTransportException(string message, Exception? innerException = null) : DnsClientException(message, innerException);