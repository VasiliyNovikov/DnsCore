using System;
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
        var responseCompletion = AddRequest(request.Id);
        await SendRequest(request, cancellationToken).ConfigureAwait(false);
        return await responseCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            try
            {
                using var responseMessage = await _transport.Receive(cancellationToken).ConfigureAwait(false);
                var response = DnsResponseEncoder.Decode(responseMessage.Buffer.Span);
                RemoveRequest(response.Id)?.TrySetResult(response);
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                RemoveAnyRequest()?.TrySetException(e);
            }
        }
    }

    private TaskCompletionSource<DnsResponse> AddRequest(ushort id)
    {
        lock (_lock)
            return CollectionsMarshal.GetValueRefOrAddDefault(_pendingRequests, id, out _) ??= new TaskCompletionSource<DnsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private TaskCompletionSource<DnsResponse>? RemoveRequest(ushort id)
    {
        lock (_lock)
            return _pendingRequests.Remove(id, out var completion) ? completion : null;
    }

    private TaskCompletionSource<DnsResponse>? RemoveAnyRequest()
    {
        lock (_lock)
        {
            ushort? firstId = null;
            foreach (var id in _pendingRequests.Keys)
            {
                firstId = id;
                break;
            }

            return firstId is null
                ? null
                : _pendingRequests.Remove(firstId.Value, out var completion) ? completion : null;
        }
    }
}