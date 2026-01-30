using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using CSnakes.Runtime;

using DnsCore.Client;
using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsClientTests
{
    private static readonly ITestServer PyServer;
    private const ushort TestDnsServerPort = 12353;

    private static DnsClientOptions GetTestDnsClientOptions(DnsTransportType transportType) =>
        new()
        {
            TransportType = transportType,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10),
            RequestTimeout = TimeSpan.FromMilliseconds(100),
            FailureRetryCount = 2
        };

    static DnsClientTests()
    {
        var builder = Host.CreateApplicationBuilder();
        var home = Path.Join(Environment.CurrentDirectory, ".");
        builder.Services
               .WithPython()
               .WithHome(home)
               .WithVirtualEnvironment(Path.Join(home, ".venv"))
               .WithUvInstaller()
               .FromRedistributable();
        var app = builder.Build();
        var env = app.Services.GetRequiredService<IPythonEnvironment>();
        PyServer = env.TestServer();
    }

    private static async Task WithTestDnsServer(Func<Task> action, IEnumerable<DnsResponse> responses)
    {
        var buffer = new byte[1024];
        var encodedResponses = new List<byte[]>();
        foreach (var response in responses)
            encodedResponses.Add(buffer[..DnsResponseEncoder.Encode(buffer, response)]);
        using var server = PyServer.Start(TestDnsServerPort, encodedResponses);
        try
        {
            await action();
        }
        finally
        {
            PyServer.Stop(server);
        }
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Resolution(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        DnsRecord expectedAnswer = new DnsAddressRecord(DnsName.Parse("test.com"), IPAddress.Parse("4.3.2.1"), TimeSpan.FromSeconds(42));
        var expectedResponse = expectedRequest.Reply(expectedAnswer);

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            var response = await client.Query(expectedRequest);
            Assert.AreEqual(DnsResponseStatus.Ok, response.Status);
            Assert.HasCount(1, response.Answers);
            DnsAssert.AreEqual(expectedAnswer, response.Answers[0]);
        }, [expectedResponse]);
    }
    
    [TestMethod]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Retry_On_Truncation(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        DnsRecord expectedAnswer = new DnsAddressRecord(DnsName.Parse("test.com"), IPAddress.Parse("4.3.2.1"), TimeSpan.FromSeconds(42));
        var truncatedResponse = expectedRequest.Reply();
        truncatedResponse.Truncated = true;
        var expectedResponse = expectedRequest.Reply(expectedAnswer);

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            var response = await client.Query(expectedRequest);
            Assert.AreEqual(DnsResponseStatus.Ok, response.Status);
            Assert.HasCount(1, response.Answers);
            DnsAssert.AreEqual(expectedAnswer, response.Answers[0]);
        }, [truncatedResponse, expectedResponse]);
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    public async Task DnsClient_Fail_On_Truncation(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        var truncatedResponse = expectedRequest.Reply();
        truncatedResponse.Truncated = true;
        var successfulResponse = expectedRequest.Reply(new DnsAddressRecord(DnsName.Parse("test.com"), IPAddress.Parse("4.3.2.1"), TimeSpan.FromSeconds(42)));

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            await Assert.ThrowsExactlyAsync<DnsResponseTruncatedException>(async () => await client.Query(expectedRequest));
        }, [truncatedResponse, successfulResponse]);
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Retry_On_Failure(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        DnsRecord expectedAnswer = new DnsAddressRecord(DnsName.Parse("test.com"), IPAddress.Parse("4.3.2.1"), TimeSpan.FromSeconds(42));
        var failureResponse = expectedRequest.Reply();
        failureResponse.Status = DnsResponseStatus.ServerFailure;
        var expectedResponse = expectedRequest.Reply(expectedAnswer);

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            var response = await client.Query(expectedRequest);
            Assert.AreEqual(DnsResponseStatus.Ok, response.Status);
            Assert.HasCount(1, response.Answers);
            DnsAssert.AreEqual(expectedAnswer, response.Answers[0]);
        }, [failureResponse, failureResponse, expectedResponse]);
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Retry_On_Failure_Fails(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        var failureResponse = expectedRequest.Reply();
        failureResponse.Status = DnsResponseStatus.ServerFailure;

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            var error = await Assert.ThrowsExactlyAsync<DnsResponseStatusException>(async () => await client.Query(expectedRequest));
            Assert.AreEqual(DnsResponseStatus.ServerFailure, error.Status);
        }, [failureResponse, failureResponse, failureResponse, expectedRequest.Reply()]);
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Resolution_Error(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("unknown.com"), DnsRecordType.A);
        var expectedResponse = expectedRequest.Reply();
        expectedResponse.Status = DnsResponseStatus.NameError;

        await WithTestDnsServer(async () =>
        {
            await using var client = new DnsClient(IPAddress.Loopback, TestDnsServerPort, GetTestDnsClientOptions(transportType));
            var error = await Assert.ThrowsExactlyAsync<DnsResponseStatusException>(async () => await client.Query(expectedRequest));
            Assert.AreEqual(DnsResponseStatus.NameError, error.Status);
        }, [expectedResponse]);
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task DnsClient_Timeout(DnsTransportType transportType)
    {
        await using var client = new DnsClient(IPAddress.Parse("203.0.113.1"), GetTestDnsClientOptions(transportType));
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await client.Query(DnsName.Parse("unknown.com"), DnsRecordType.A));
    }
}