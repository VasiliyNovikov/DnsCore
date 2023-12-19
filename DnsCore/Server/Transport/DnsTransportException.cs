using System;

namespace DnsCore.Server.Transport;

public class DnsTransportException(string message, Exception innerException) : Exception(message, innerException);