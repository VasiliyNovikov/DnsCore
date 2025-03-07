using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<DnsResponse>> _pendingRequests = [];


    public DnsSimpleResolver(DnsTransportType transportType, EndPoint endPoint)
    {
        _transport = DnsClientTransport.Create(transportType, endPoint);
        _receiveTask = ReceiveResponses(_receiveTaskCancellation.Token);
    }

    public override async ValueTask<DnsResponse> Resolve(DnsRequest request, CancellationToken cancellationToken)
    {
        var responseCompletion = new TaskCompletionSource<DnsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(request.Id, responseCompletion))
            throw new DnsClientException("Request ID collision");

        try
        {
            try
            {
                await SendRequest(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                responseCompletion.TrySetCanceled(cancellationToken);
            }
            return await responseCompletion.Task.ConfigureAwait(false);
        }
        finally
        {
            _pendingRequests.TryRemove(new(request.Id, responseCompletion));
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
            if (_pendingRequests.TryGetValue(response.Id, out var completion))
                completion.TrySetResult(response);
        }
    }
}