using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace DnsCore.Client;

public static class SystemDnsConfiguration
{
    public static IEnumerable<EndPoint> GetEndPoints()
    {
        foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            if (@interface.OperationalStatus == OperationalStatus.Up)
                foreach (var dnsAddress in @interface.GetIPProperties().DnsAddresses)
                    yield return new IPEndPoint(dnsAddress, DnsDefaults.Port);
    }
}