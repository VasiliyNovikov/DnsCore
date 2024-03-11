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
        CollectionAssert.AllItemsAreNotNull(addresses);
        CollectionAssert.AllItemsAreUnique(addresses);
    }
}