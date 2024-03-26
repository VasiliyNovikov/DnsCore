using System;

using DnsCore.Common;

namespace DnsCore.Server.Transport;

internal sealed class DnsServerTransportException(string message, Exception? innerException = null) : DnsException(message, innerException);