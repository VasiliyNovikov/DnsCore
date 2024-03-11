using System;

using DnsCore.Common;

namespace DnsCore.Client;

public class DnsClientException(string message, Exception? innerException = null) : DnsException(message, innerException);