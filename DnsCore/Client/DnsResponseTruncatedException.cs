using DnsCore.Model;

namespace DnsCore.Client;

public sealed class DnsResponseTruncatedException(DnsResponse response) : DnsResponseException("DNS response was truncated", response);
