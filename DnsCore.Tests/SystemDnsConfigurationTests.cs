using System.Net;

using DnsCore.Client;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class SystemDnsConfigurationTests
{
    [TestMethod]
    public void SystemDnsConfiguration_GetEndPoints()
    {
        EndPoint[] endPoints = [.. SystemDnsConfiguration.GetEndPoints()];
        Assert.IsTrue(endPoints.Length > 0);
        foreach (var endPoint in endPoints)
        {
            Assert.IsNotNull(endPoint);
            Assert.IsInstanceOfType<IPEndPoint>(endPoint, out var ipEndPoint);
            Assert.AreEqual(DnsDefaults.Port, ipEndPoint.Port);
        }
    }
}