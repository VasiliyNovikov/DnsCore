using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace DnsCore.Client;

public static class SystemDnsConfiguration
{
    public static IPAddress[] GetAddresses()
    {
        HashSet<IPAddress> addresses = [];
        foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            if (@interface.OperationalStatus == OperationalStatus.Up)
                foreach (var address in @interface.GetIPProperties().DnsAddresses)
                    addresses.Add(address);
        return [.. addresses];
    }
}