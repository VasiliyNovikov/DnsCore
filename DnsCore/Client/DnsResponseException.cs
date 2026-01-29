using DnsCore.Model;

namespace DnsCore.Client;

public class DnsResponseException(string message, DnsResponse response) : DnsClientException(message)
{
    public DnsResponse Response => response;
}