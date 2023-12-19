using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;
using DnsCore.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var address = IPAddress.Parse(args[0]);
var port = args.Length < 2 ? (ushort)53 : ushort.Parse(args[1], CultureInfo.InvariantCulture);

var records = new DnsRecord[]
{
    new DnsCNameRecord(DnsName.Parse("example.com"), DnsName.Parse("www.example.com"), TimeSpan.FromSeconds(24)),
    new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42)),
    new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(4242))
};

Console.WriteLine("Test DNS server");
Console.WriteLine("Records:");
foreach (var record in records)
    Console.WriteLine($"\t{record}");
Console.WriteLine("Press any key to exit...");

var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
    .BuildServiceProvider();

using var server = new DnsUdpServer(address, port, HandleRequest, services.GetRequiredService<ILogger<DnsUdpServer>>());
await server.Run();
return;

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