using System;

namespace DnsCore.Common;

public class DnsSocketException(string message, Exception? innerException = null) : DnsException(message, innerException);