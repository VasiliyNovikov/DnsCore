using System;
using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

using DnsCore;
using DnsCore.Client;
using DnsCore.Common;
using DnsCore.Model;

var serverOption = new Option<string>("-s", "--server") { Description = "DNS server address", Arity = ArgumentArity.ZeroOrOne };
var portOption = new Option<ushort?>("-p", "--port") { Description = "DNS server port", Arity = ArgumentArity.ZeroOrOne };
var typeOption = new Option<DnsRecordType?>("-t", "--type") { Description = "DNS record type", Arity = ArgumentArity.ZeroOrOne };
var queryOption = new Option<string>("-q", "--query") { Description = "DNS query", Arity = ArgumentArity.ExactlyOne };

var rootCommand = new RootCommand("Test DNS Client");
rootCommand.Options.Add(serverOption);
rootCommand.Options.Add(portOption);
rootCommand.Options.Add(typeOption);
rootCommand.Options.Add(queryOption);

rootCommand.SetAction(async parseResult =>
{
    var server = parseResult.GetValue(serverOption) is { } serverStr ? IPAddress.Parse(serverStr) : null;
    var port = parseResult.GetValue(portOption);
    var type = parseResult.GetValue(typeOption);
    var query = parseResult.GetRequiredValue(queryOption);
    await Run(server, port, type, query);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task Run(IPAddress? server, ushort? port, DnsRecordType? type, string query)
{
    var effectivePort = port ?? DnsDefaults.Port;
    var effectiveType = type ?? DnsRecordType.A;
    var request = new DnsRequest(DnsName.Parse(query), effectiveType);

    if (server is not null)
        Console.WriteLine($"Server: {server}:{effectivePort}");
    Console.WriteLine($"Request:\n{request}");

    await using var client = server is null ? new DnsClient() : new DnsClient(DnsTransportType.All, server, effectivePort);

    try
    {
        var timer = Stopwatch.StartNew();
        var response = await client.Query(DnsName.Parse(query), effectiveType);
        timer.Stop();
        Console.WriteLine($"Response:\n{response}");
        Console.WriteLine($"Time taken: {(int)timer.ElapsedMilliseconds} ms");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Request timed out");
    }
    catch (DnsClientException e)
    {
        Console.WriteLine($"Request failed: {e.Message}");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}