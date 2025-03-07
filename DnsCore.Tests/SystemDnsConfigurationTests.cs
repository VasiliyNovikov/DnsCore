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
        Assert.IsTrue(addresses.Length > 0);
        CollectionAssert.AllItemsAreNotNull(addresses);
        CollectionAssert.AllItemsAreUnique(addresses);
    }
}