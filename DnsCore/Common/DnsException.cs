using System;

namespace DnsCore.Common;

public class DnsException(string message, Exception? innerException = null) : Exception(message, innerException);