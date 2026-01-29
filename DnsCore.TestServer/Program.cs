using System;
using System.CommandLine;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore;
using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Server.Hosting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var addressOption = new Option<string>("-a", "--address") { Description = "DNS server address", Arity = ArgumentArity.ZeroOrOne };
var portOption = new Option<ushort?>("-p", "--port") { Description = "DNS server port", Arity = ArgumentArity.ZeroOrOne };
var transportOption = new Option<DnsTransportType?>("-t", "--transport") { Description = "DNS transport type", Arity = ArgumentArity.ZeroOrOne };

var rootCommand = new RootCommand("Test DNS Client");
rootCommand.Options.Add(addressOption);
rootCommand.Options.Add(portOption);
rootCommand.Options.Add(transportOption);

var records = new DnsRecord[]
{
    new DnsCNameRecord(DnsName.Parse("example.com"), DnsName.Parse("www.example.com"), TimeSpan.FromSeconds(24)),
    new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42)),
    new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(4242)),
    new DnsTextRecord(DnsName.Parse("www.example.com"), "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", TimeSpan.FromSeconds(424242))
};

rootCommand.SetAction(async parseResult =>
{
    var address = parseResult.GetValue(addressOption) is { } addressStr ? IPAddress.Parse(addressStr) : null;
    var port = parseResult.GetValue(portOption) ?? DnsDefaults.Port;
    var transport = parseResult.GetValue(transportOption) ?? DnsTransportType.All;

    Console.WriteLine("Test DNS server");
    Console.WriteLine("Records:");
    foreach (var record in records)
        Console.WriteLine($"\t{record}");
    Console.WriteLine("Press any key to exit...");

    var builder = Host.CreateApplicationBuilder(args);
    if (address is null)
        builder.Services.AddDns(port, transport, HandleRequest);
    else
        builder.Services.AddDns(address, port, transport, HandleRequest);
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);
    using var host = builder.Build();
    await host.RunAsync();
});

return await rootCommand.Parse(args).InvokeAsync();

async ValueTask<DnsResponse> HandleRequest(DnsRequest request, CancellationToken cancellationToken)
{
    await Task.Yield();

    DnsResponse response;
    if (request.Questions.Count == 0)
    {
        response = request.Reply();
        response.Status = DnsResponseStatus.FormatError;
    }
    else
    {
        var question = request.Questions[0];
        var answers = records.Where(r => r.Name == question.Name && (r.RecordType == question.RecordType || r.RecordType == DnsRecordType.CNAME)).ToArray();
        response = request.Reply(answers);
        if (answers.Length == 0)
            response.Status = DnsResponseStatus.NameError;
    }
    return response;
}