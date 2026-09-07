using DnsCore.Client;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class SystemDnsConfigurationTests
{
    [TestMethod]
    public void SystemDnsConfiguration_GetAddresses()
    {
        var addresses = SystemDnsConfiguration.GetAddresses();
        Assert.IsNotEmpty(addresses);
        Assert.AreAllNotNull(addresses);
        Assert.AreAllDistinct(addresses);
    }
}