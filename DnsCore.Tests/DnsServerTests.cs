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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsServerTests
{
    private static readonly IPAddress ServerAddress = IPAddress.Loopback;
    private static readonly ushort Port = (ushort)(OperatingSystem.IsWindows() ? 53 : 5553);

    public async Task Do_Test_Request_Response(DnsQuestion question, params DnsRecord[] answers)
    {
        DnsQuestion actualQuestion = null!;

        using var server = new DnsUdpServer(ServerAddress, Port, ProcessRequest);

        var actualAnswers = await Resolve(question.Name.ToString(), question.RecordType);

        Assert.AreEqual(question.Name, actualQuestion.Name);
        Assert.AreEqual(question.RecordType, actualQuestion.RecordType);
        Assert.AreEqual(question.Class, actualQuestion.Class);

        Assert.AreEqual(answers.Length, actualAnswers.Count);
        for (var i = 0; i < answers.Length; ++i)
        {
            var answer = answers[i];
            var actualAnswer = actualAnswers[i];
            Assert.AreEqual(answer.Name, actualAnswer.Name);
            Assert.AreEqual(answer.RecordType, actualAnswer.RecordType);
            Assert.AreEqual(answer.Class, actualAnswer.Class);
            Assert.AreEqual(answer.Ttl, actualAnswer.Ttl);

            switch (answer.RecordType)
            {
                case DnsRecordType.A:
                case DnsRecordType.AAAA:
                    Assert.IsTrue(((DnsAddressRecord)answer).Address.Equals(((DnsAddressRecord)actualAnswer).Address));
                    break;
                case DnsRecordType.CNAME:
                    Assert.AreEqual(((DnsCNameRecord)answer).Alias, ((DnsCNameRecord)actualAnswer).Alias);
                    break;
                case DnsRecordType.PTR:
                    Assert.AreEqual(((DnsPtrRecord)answer).PtrName, ((DnsPtrRecord)actualAnswer).PtrName);
                    break;
                default:
                    CollectionAssert.AreEqual(((DnsRawRecord)answer).Data, ((DnsRawRecord)actualAnswer).Data);
                    break;
            }
        }

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

    private static async Task<List<DnsRecord>> Resolve(string name, DnsRecordType type)
    {
        if (OperatingSystem.IsWindows())
            return await ResolveWindows(name, type);
        else if (OperatingSystem.IsLinux())
            return await ResolveLinux(name, type);
        else
            throw new PlatformNotSupportedException();
    }

    private sealed record class PowerShellDnsRecord(string Name, DnsRecordType Type, int TTL, string? Address, string? NameHost);
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
            }
        }
        return result;
    }

    private static async Task<List<DnsRecord>> ResolveLinux(string name, DnsRecordType type)
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