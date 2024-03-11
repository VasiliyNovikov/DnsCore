using System;
using System.CommandLine;
using System.Net;
using System.Threading.Tasks;

using DnsCore;
using DnsCore.Client;
using DnsCore.Common;
using DnsCore.Model;

var addressOption = new Option<IPAddress>(aliases: ["-s", "--server"], description: "DNS server address")  { IsRequired = true };
var portOption = new Option<ushort?>(aliases: ["-p", "--port"], description: "DNS server port");
var typeOption = new Option<DnsRecordType?>(aliases: ["-t", "--type"], description: "DNS record type");
var queryOption = new Option<string>(aliases: ["-q", "--query"], description: "DNS query")  { IsRequired = true };

var rootCommand = new RootCommand("Test DNS Client");
rootCommand.AddOption(addressOption);
rootCommand.AddOption(portOption);
rootCommand.AddOption(typeOption);
rootCommand.AddOption(queryOption);

rootCommand.SetHandler(Run, addressOption, portOption, typeOption, queryOption);

return await rootCommand.InvokeAsync(args);

static async Task Run(IPAddress address, ushort? port, DnsRecordType? type, string query)
{
    var effectivePort = port ?? DnsDefaults.Port;
    var effectiveType = type ?? DnsRecordType.A;
    var request = new DnsRequest(DnsName.Parse(query), effectiveType);

    Console.WriteLine($"Server: {address}:{effectivePort}");
    Console.WriteLine($"Request:\n{request}");

    await using var client = new DnsClient(DnsTransportType.UDP,  address, effectivePort);

    try
    {
        var response = await client.Query(request);
        Console.WriteLine($"Response:\n{response}");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}