using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;
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

    private static async Task Do_Test_Request_Response(DnsTransportType transportType, DnsQuestion question, params DnsRecord[] answers)
    {
        DnsQuestion? actualQuestion = null;

        await using var server = new DnsServer(ServerAddress, Port, transportType, ProcessRequest, Logger);
        server.Start();

        bool[] useTcpParams = transportType >= DnsTransportType.All ? [false, true] : [transportType == DnsTransportType.TCP];
        foreach (var useTcp in useTcpParams)
        {
            var actualAnswers = await Resolve(question.Name.ToString(), question.RecordType, useTcp);

            Assert.AreEqual(question, actualQuestion);
            Assert.AreEqual(answers.Length, actualAnswers.Count);
            for (var i = 0; i < answers.Length; ++i)
                DnsAssert.AreEqual(answers[i], actualAnswers[i]);
        }

        return;

        ValueTask<DnsResponse> ProcessRequest(DnsRequest request, CancellationToken token)
        {
            actualQuestion = request.Questions[0];
            return ValueTask.FromResult(request.Reply(answers));
        }
    }

    [TestMethod]
    [DataRow(DnsTransportType.UDP)]
    [DataRow(DnsTransportType.TCP)]
    [DataRow(DnsTransportType.All)]
    public async Task Test_Request_Response(DnsTransportType transportType)
    {
        await Do_Test_Request_Response(transportType,
                                       new(DnsName.Parse("alias.example.com"), DnsRecordType.A),
                                       new DnsCNameRecord(DnsName.Parse("alias.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                                       new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(transportType,
                                       new(DnsName.Parse("host.example.com"), DnsRecordType.AAAA),
                                       new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(transportType,
                                       new(DnsName.Parse("alias.example.com"), DnsRecordType.CNAME),
                                       new DnsCNameRecord(DnsName.Parse("alias.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)));
        await Do_Test_Request_Response(transportType,
                                       new(DnsName.Parse("unknown.example.com"), DnsRecordType.A));
        await Do_Test_Request_Response(transportType,
                                       new(DnsName.Parse("txt.example.com"), DnsRecordType.TXT),
                                       new DnsTextRecord(DnsName.Parse("txt.example.com"), "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", TimeSpan.FromSeconds(42)));
    }

    private static async Task<List<DnsRecord>> Resolve(string name, DnsRecordType type, bool useTcp)
    {
        return OperatingSystem.IsWindows()
            ? await ResolveWindows(name, type, useTcp)
            : await ResolveUnix(name, type, useTcp);
    }

    private sealed record PowerShellDnsRecord(string Name, DnsRecordType Type, int TTL, string? Address, string? NameHost, string[] Strings);
    private static async Task<List<DnsRecord>> ResolveWindows(string name, DnsRecordType type, bool useTcp)
    {
        string output;
        try
        {
            output = await Command("pwsh", "-Command", $"Resolve-DnsName -Name {name} -Server '{ServerAddress}' -Type {type} {(useTcp ? "-TcpOnly" : "")} | ConvertTo-Json");
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
                case DnsRecordType.TXT:
                    result.Add(new DnsTextRecord(recordName, String.Join("", powerShellRecord.Strings), ttl));
                    break;
            }
        }
        return result;
    }

    private static async Task<List<DnsRecord>> ResolveUnix(string name, DnsRecordType type, bool useTcp)
    {
        var output = await Command("dig", $"@{ServerAddress}", "-p", Port.ToString(), "-t", type.ToString(), useTcp ? "+tcp" : "+notcp", "+nocmd", "+noall", "+answer", "+nostats", name);
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
                case DnsRecordType.TXT:
                    result.Add(new DnsTextRecord(recordName, answerStr.Replace("\" \"", "").Replace("\"", ""), ttl));
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
            StartInfo = new ProcessStartInfo(name)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

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