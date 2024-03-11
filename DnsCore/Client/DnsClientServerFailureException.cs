using DnsCore.Model;

namespace DnsCore.Client;

public sealed class DnsResponseStatusException(DnsResponseStatus status) : DnsClientException($"Server returned error status: {status}")
{
    public DnsResponseStatus Status => status;
}