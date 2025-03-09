using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Client.Transport;
using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;

namespace DnsCore.Client.Resolver;

internal class DnsSimpleResolver : DnsResolver
{
    private readonly DnsClientTransport _transport;
    private readonly CancellationTokenSource _receiveTaskCancellation = new();
    private readonly Task _receiveTask;
    private readonly Lock _lock = new();
    private readonly Dictionary<ushort, TaskCompletionSource<DnsResponse>> _pendingRequests = [];


    public DnsSimpleResolver(DnsTransportType transportType, EndPoint endPoint)
    {
        _transport = DnsClientTransport.Create(transportType, endPoint);
        _receiveTask = ReceiveResponses(_receiveTaskCancellation.Token);
    }

    public override async ValueTask<DnsResponse> Resolve(DnsRequest request, CancellationToken cancellationToken)
    {
        var responseCompletion = AddRequest(request.Id, out var exists);
        try
        {
            await SendRequest(request, cancellationToken).ConfigureAwait(false);
            return await responseCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!exists)
                RemoveRequest(request.Id);
        }
    }

    [SuppressMessage("Microsoft.Usage", "CA1816: Dispose methods should call SuppressFinalize")]
    public override async ValueTask DisposeAsync()
    {
        await _receiveTaskCancellation.CancelAsync().ConfigureAwait(false);
        await _receiveTask.ConfigureAwait(false);
        _receiveTaskCancellation.Dispose();
        await _transport.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask SendRequest(DnsRequest request, CancellationToken cancellationToken)
    {
        using var buffer = new DnsTransportBuffer(DnsDefaults.MaxUdpMessageSize);
        var len = DnsRequestEncoder.Encode(buffer.Span, request);
        buffer.Resize(len);
        using var requestMessage = new DnsTransportMessage(buffer);
        await _transport.Send(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReceiveResponses(CancellationToken cancellationToken)
    {
        while (true)
        {
            using var responseMessage = await _transport.Receive(cancellationToken).ConfigureAwait(false);
            var response = DnsResponseEncoder.Decode(responseMessage.Buffer.Span);
            GetRequest(response.Id)?.TrySetResult(response);
        }
    }

    private TaskCompletionSource<DnsResponse> AddRequest(ushort id, out bool exists)
    {
        lock (_lock)
            return CollectionsMarshal.GetValueRefOrAddDefault(_pendingRequests, id, out exists) ??= new TaskCompletionSource<DnsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private TaskCompletionSource<DnsResponse>? GetRequest(ushort id)
    {
        lock (_lock)
            return _pendingRequests.GetValueOrDefault(id);
    }

    private void RemoveRequest(ushort id)
    {
        lock (_lock)
            _pendingRequests.Remove(id);
    }
}