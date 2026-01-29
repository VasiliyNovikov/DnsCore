using DnsCore.Common;

namespace DnsCore.Client;

public class DnsClientException(string message) : DnsException(message);