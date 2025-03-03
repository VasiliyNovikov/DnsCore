using System;

namespace DnsCore.Server.Transport;

internal sealed class DnsServerTransportException(string message, Exception? innerException = null) : Exception(message, innerException);