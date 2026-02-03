using System.Threading;
using System.Threading.Tasks;

using DnsCore.Model;

namespace DnsCore.Server;

public interface IDnsServerHandler
{
    ValueTask<DnsResponse> Handle(DnsRequest request, CancellationToken cancellationToken);
}