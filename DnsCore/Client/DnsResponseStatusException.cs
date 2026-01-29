using DnsCore.Model;

namespace DnsCore.Client;

public sealed class DnsResponseStatusException(DnsResponse response) : DnsResponseException($"Server returned error status: {response.Status}", response)
{
    public DnsResponseStatus Status => Response.Status;
}