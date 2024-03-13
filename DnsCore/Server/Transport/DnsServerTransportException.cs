using System;

namespace DnsCore.Server.Transport;

public class DnsServerTransportException(string message, Exception? innerException = null) : Exception(message, innerException);