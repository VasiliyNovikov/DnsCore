using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;
using DnsCore.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsServerTests
{
    private static readonly IPAddress ServerAddress = IPAddress.Loopback;
    private static readonly ushort Port = (ushort)(OperatingSystem.IsWindows() ? DnsDefaults.Port : 5553);
    private static readonly ILogger Logger;

    static DnsServerTests()
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder
                .AddSimpleConsole(options => options.TimestampFormat = "hh:mm:ss.ff ")
                .SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider();
        Logger = services.GetRequiredService<ILogger<DnsServerTests>>();
    }

    private static void AssertRecordsEqual(DnsRecord expected, DnsRecord actual)
    {
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.RecordType, actual.RecordType);
        Assert.AreEqual(expected.Class, actual.Class);
        Assert.AreEqual(expected.Ttl, actual.Ttl);
        switch (expected.RecordType)
        {
            case DnsRecordType.A:
            case DnsRecordType.AAAA:
                Assert.IsTrue(((DnsAddressRecord)expected).Data.Equals(((DnsAddressRecord)actual).Data));
                break;
            case DnsRecordType.CNAME:
            case DnsRecordType.PTR:
                Assert.AreEqual(((DnsNameRecord)expected).Data, ((DnsNameRecord)actual).Data);
                break;
            default:
                CollectionAssert.AreEqual(((DnsRawRecord)expected).Data, ((DnsRawRecord)actual).Data);
                break;
        }
    }

    private static async Task Do_Test_Request_Response(DnsQuestion question, params DnsRecord[] answers)
    {
        DnsQuestion? actualQuestion = null;

        await using var server = new DnsServer(ServerAddress, Port, DnsTransportType.UDP, ProcessRequest, Logger);
        server.Start();

        var actualAnswers = await Resolve(question.Name.ToString(), question.RecordType);

        Assert.AreEqual(question, actualQuestion);
        Assert.AreEqual(answers.Length, actualAnswers.Count);
        for (var i = 0; i < answers.Length; ++i)
            AssertRecordsEqual(answers[i], actualAnswers[i]);
        return;

        ValueTask<DnsResponse> ProcessRequest(DnsRequest request, CancellationToken token)
        {
            actualQuestion = request.Questions[0];
            return ValueTask.FromResult(request.Reply(answers));
        }
    }

    [TestMethod]
    public async Task Test_Request_Response()
    {
        await Do_Test_Request_Response(new(DnsName.Parse("alias.example.com"), DnsRecordType.A),
                                       new DnsCNameRecord(DnsName.Parse("alias.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                                       new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(new(DnsName.Parse("host.example.com"), DnsRecordType.AAAA),
                                       new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(new(DnsName.Parse("alias.example.com"), DnsRecordType.CNAME),
                                       new DnsCNameRecord(DnsName.Parse("alias.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(new(DnsName.Parse("unknown.example.com"), DnsRecordType.A));
    }
    
    [TestMethod]
    public void Test_Encode_Decode()
    {
        List<DnsRequest> requests = [
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.AAAA),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.CNAME),
            new DnsRequest(DnsName.Parse("unknown.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsRecordType.PTR),
            new DnsRequest(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsRecordType.PTR)
        ];

        List<DnsResponse> responses = [
            requests[0].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42))),
            
            requests[1].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(42))),

            requests[2].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[3].Reply(DnsResponseStatus.NameError),

            requests[4].Reply(new DnsPtrRecord(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[5].Reply(new DnsPtrRecord(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)))
        ];

        List<DnsMessage> messages = [.. requests, .. responses];
        
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize]; 
        foreach (var message in messages)
        {
            var length = message.Encode(buffer);
            var messageSpan = buffer[..length];
            
            DnsMessage actualMessage = message is DnsRequest ? DnsRequest.Decode(messageSpan) : DnsResponse.Decode(messageSpan);
            
            Assert.AreEqual(message.Id, actualMessage.Id);
            Assert.AreEqual(message.RequestType, actualMessage.RequestType);
            Assert.AreEqual(message.RecursionDesired, actualMessage.RecursionDesired);
            Assert.AreEqual(message.Questions.Count, actualMessage.Questions.Count);
            for (var i = 0; i < message.Questions.Count; ++i)
                Assert.AreEqual(message.Questions[i], actualMessage.Questions[i]);

            if (message is DnsRequest) 
                Assert.IsInstanceOfType<DnsRequest>(actualMessage);
            else
            {
                var response = (DnsResponse)message;
                var actualResponse = (DnsResponse)actualMessage;
                Assert.AreEqual(response.Status, actualResponse.Status);
                Assert.AreEqual(response.RecursionAvailable, actualResponse.RecursionAvailable);
                Assert.AreEqual(response.AuthoritativeAnswer, actualResponse.AuthoritativeAnswer);
                Assert.AreEqual(response.Truncated, actualResponse.Truncated);
                Assert.AreEqual(response.Answers.Count, actualResponse.Answers.Count);
                for (var i = 0; i < response.Answers.Count; ++i)
                    AssertRecordsEqual(response.Answers[i], actualResponse.Answers[i]);
                Assert.AreEqual(response.Authorities.Count, actualResponse.Authorities.Count);
                for (var i = 0; i < response.Authorities.Count; ++i)
                    AssertRecordsEqual(response.Authorities[i], actualResponse.Authorities[i]);
                Assert.AreEqual(response.Additional.Count, actualResponse.Additional.Count);
                for (var i = 0; i < response.Additional.Count; ++i)
                    AssertRecordsEqual(response.Additional[i], actualResponse.Additional[i]);
            }

            Assert.AreEqual(length, message.Encode(buffer[..length]));

            for (var l = 0; l < length; ++l)
            {
                var smallLength = l;
                Assert.ThrowsException<FormatException>(() =>
                {
                    Span<byte> smallBuffer = stackalloc byte[smallLength]; 
                    message.Encode(smallBuffer);
                });

                Assert.ThrowsException<FormatException>(() =>
                {
                    Span<byte> buffer = stackalloc byte[length];
                    message.Encode(buffer);

                    var smallBuffer = buffer[..smallLength];
                    if (message is DnsRequest)
                        DnsRequest.Decode(smallBuffer);
                    else
                        DnsResponse.Decode(smallBuffer);
                });
            }
        }
    }

    [TestMethod]
    public void Test_Decode_Malformed()
    {
        Memory<byte> buffer = new byte[DnsDefaults.MaxUdpMessageSize];
        for (var i = 0; i < 1000; ++i)
        {
            var messageMem = buffer[..Random.Shared.Next(0, DnsDefaults.MaxUdpMessageSize)];
            Random.Shared.NextBytes(messageMem.Span);
            Assert.ThrowsException<FormatException>(() => DnsRequest.Decode(messageMem.Span));
            Assert.ThrowsException<FormatException>(() => DnsResponse.Decode(messageMem.Span));
        }
    }

    private static async Task<List<DnsRecord>> Resolve(string name, DnsRecordType type)
    {
        return OperatingSystem.IsWindows()
            ? await ResolveWindows(name, type)
            : await ResolveUnix(name, type);
    }

    private sealed record PowerShellDnsRecord(string Name, DnsRecordType Type, int TTL, string? Address, string? NameHost);
    private static async Task<List<DnsRecord>> ResolveWindows(string name, DnsRecordType type)
    {
        string output;
        try
        {
            output = await Command("pwsh", "-Command", $"Resolve-DnsName -Name {name} -Server '{ServerAddress}' -Type {type} | ConvertTo-Json");
        }
        catch (CommandException)
        {
            return [];
        }

        var powerShellDnsResult = output.StartsWith('[')
            ? JsonSerializer.Deserialize<List<PowerShellDnsRecord>>(output)!
            : [JsonSerializer.Deserialize<PowerShellDnsRecord>(output)!];
        List<DnsRecord> result = [];
        foreach (var powerShellRecord in powerShellDnsResult)
        {
            var recordName = DnsName.Parse(powerShellRecord.Name);
            var recordType = powerShellRecord.Type;
            var ttl = TimeSpan.FromSeconds(powerShellRecord.TTL);
            switch (recordType)
            {
                case DnsRecordType.A:
                case DnsRecordType.AAAA:
                    result.Add(new DnsAddressRecord(recordName, IPAddress.Parse(powerShellRecord.Address!), ttl));
                    break;
                case DnsRecordType.CNAME:
                    result.Add(new DnsCNameRecord(recordName, DnsName.Parse(powerShellRecord.NameHost!), ttl));
                    break;
                case DnsRecordType.PTR:
                    result.Add(new DnsPtrRecord(recordName, DnsName.Parse(powerShellRecord.NameHost!), ttl));
                    break;
            }
        }
        return result;
    }

    private static async Task<List<DnsRecord>> ResolveUnix(string name, DnsRecordType type)
    {
        var output = await Command("dig", $"@{ServerAddress}", "-p", Port.ToString(), "-t", type.ToString(), "+nocmd", "+noall", "+answer", "+nostats", name);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<DnsRecord>(lines.Length);
        foreach (var line in lines)
        {
            var fields = line.Split('\t');
            var recordName = DnsName.Parse(fields[0]);
            var recordType = Enum.Parse<DnsRecordType>(fields[3]);
            var ttl = TimeSpan.FromSeconds(int.Parse(fields[1]));
            var answerStr = fields[4];
            switch (recordType)
            {
                case DnsRecordType.A:
                case DnsRecordType.AAAA:
                    result.Add(new DnsAddressRecord(recordName, IPAddress.Parse(answerStr), ttl));
                    break;
                case DnsRecordType.CNAME:
                    result.Add(new DnsCNameRecord(recordName, DnsName.Parse(answerStr), ttl));
                    break;
                case DnsRecordType.PTR:
                    result.Add(new DnsPtrRecord(recordName, DnsName.Parse(answerStr), ttl));
                    break;
            }
        }
        return result;
    }

    private static async Task<string> Command(string name, params string[] args)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(name, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.OutputDataReceived += (_, e) => HandleDataReceived(output, e);
        process.ErrorDataReceived += (_, e) => HandleDataReceived(error, e);

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new CommandException(error.ToString().Trim());

        return output.ToString().Trim();

        static void HandleDataReceived(StringBuilder target, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;
            if (target.Length > 0)
                target.Append('\n');
            target.Append(e.Data);
        }
    }

    private sealed class CommandException(string message) : Exception(message);
}