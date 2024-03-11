using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

using DnsCore.Client;
using DnsCore.Common;
using DnsCore.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsClientTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(5);
    private const ushort TestDnsServerPort = 12353;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private static async Task WithTestDnsServer(Func<Task> action, IEnumerable<DnsResponse> responses)
    {
        Assert.IsTrue(File.Exists("test_server.py"), "test_server.py not found");

        var messagesJsonObj = responses.Select(r => new
                                       {
                                           Question = new
                                           {
                                               Name = r.Questions[0].Name.ToString(),
                                               Type = r.Questions[0].RecordType.ToString()
                                           },
                                           Answers = r.Answers.Select(a => new
                                                              {
                                                                  Name = a.Name.ToString(),
                                                                  Type = a.RecordType.ToString(),
                                                                  Ttl = (int)a.Ttl.TotalSeconds,
                                                                  a.Data
                                                              })
                                                              .ToList()
                                       })
                                       .ToList();

        var messagesFileName = Path.GetTempFileName();
        await using (var tempFile = File.OpenWrite(messagesFileName))
            await JsonSerializer.SerializeAsync(tempFile, messagesJsonObj, JsonOptions);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"test_server.py {TestDnsServerPort} {messagesFileName}",
            }
        };

        process.Start();

        var startupStartTime = DateTime.Now;
        var retryDelay = TimeSpan.FromMilliseconds(1);
        while (DateTime.Now < startupStartTime + StartupTimeout)
        {
            await Task.Delay(retryDelay);
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                await socket.ConnectAsync(IPAddress.Loopback, TestDnsServerPort);
                break;
            }
            catch (Exception)
            {
                // Ignore
            }
            retryDelay *= 2;
        }

        try
        {
            Assert.IsFalse(process.HasExited);
            await action();
        }
        finally
        {
            process.Kill();
            await process.WaitForExitAsync();
            File.Delete(messagesFileName);
        }
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    //[DataRow(DnsTransportType.All)]
    public async Task TestDnsClient(DnsTransportType transportType)
    {
        var expectedRequest = new DnsRequest(DnsName.Parse("test.com"), DnsRecordType.A);
        DnsRecord expectedAnswer = new DnsAddressRecord(DnsName.Parse("test.com"), IPAddress.Parse("4.3.2.1"), TimeSpan.FromSeconds(42));
        var expectedResponse = expectedRequest.Reply(expectedAnswer);        

        await WithTestDnsServer(async () =>
        {
            var client = new DnsClient(transportType, IPAddress.Loopback, TestDnsServerPort);

            var response = await client.Query(expectedRequest);
            Assert.AreEqual(DnsResponseStatus.Ok, response.Status);
            Assert.AreEqual(1, response.Answers.Count);

            var answer = response.Answers[0];
            Assert.AreEqual(expectedAnswer.Name, answer.Name);
            Assert.AreEqual(expectedAnswer.RecordType, answer.RecordType);
            Assert.AreEqual(expectedAnswer.Class, answer.Class);
            Assert.AreEqual(expectedAnswer.Ttl, answer.Ttl);
            Assert.AreEqual(expectedAnswer.Data, answer.Data);

            var errorResponse = await client.Query(DnsName.Parse("unknown.com"), DnsRecordType.A);
            Assert.AreEqual(DnsResponseStatus.NameError, errorResponse.Status);
        }, [expectedResponse]);
    }
}