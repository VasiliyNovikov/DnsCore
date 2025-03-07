using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Client.Transport;
using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;

namespace DnsCore.Client;

public sealed class DnsClient : IAsyncDisposable
{
    private const int InitialRetryDelayMilliseconds = 250;
    private const int DefaultRequestTimeoutMilliseconds = 5000;

    private readonly TimeSpan _requestTimeout;
    private readonly DnsClientTransport _transport;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<DnsResponse>> _pendingRequests = [];
    private readonly CancellationTokenSource _receiveTaskCancellation = new();
    private readonly Task _receiveTask;

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint, TimeSpan requestTimeout)
    {
        _requestTimeout = requestTimeout;
        _transport = DnsClientTransport.Create(transportType, serverEndPoint);
        _receiveTask = ReceiveResponses(_receiveTaskCancellation.Token);
    }

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint) : this(transportType, serverEndPoint, TimeSpan.FromMilliseconds(DefaultRequestTimeoutMilliseconds)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port, TimeSpan requestTimeout) : this(transportType, new IPEndPoint(serverAddress, port), requestTimeout) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port = DnsDefaults.Port) : this(transportType, new IPEndPoint(serverAddress, port)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, TimeSpan requestTimeout) : this(transportType, serverAddress, DnsDefaults.Port, requestTimeout) {}

    public async ValueTask DisposeAsync()
    {
        await _receiveTaskCancellation.CancelAsync();
        await _receiveTask;
        _receiveTaskCancellation.Dispose();
        await _transport.DisposeAsync();
    }

    public async ValueTask<DnsResponse> Query(DnsRequest request, CancellationToken cancellationToken = default)
    {
        using var timeoutCancellation = new CancellationTokenSource(_requestTimeout);
        using var aggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
        var responseCompletion = AddRequest(request.Id);
        try
        {
            await using var cancellationRegistration = aggregatedCancellation.Token.Register(() => responseCompletion.TrySetCanceled()).ConfigureAwait(false);
            await SendRequest(request, aggregatedCancellation.Token).ConfigureAwait(false);
            return await responseCompletion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException e) when (e.CancellationToken == timeoutCancellation.Token)
        {
            throw new TimeoutException("DNS request timed out");
        }
        finally
        {
            RemoveRequest(request.Id, responseCompletion);
        }
    }

    public async ValueTask<DnsResponse> Query(DnsName name, DnsRecordType type, CancellationToken cancellationToken = default)
    {
        return await Query(new DnsRequest(name, type), cancellationToken).ConfigureAwait(false);
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
        await Task.Yield();
        while (true)
        {
            using var responseMessage = await _transport.Receive(cancellationToken).ConfigureAwait(false);
            CompleteRequest(DnsResponseEncoder.Decode(responseMessage.Buffer.Span));
        }
    }

    private TaskCompletionSource<DnsResponse> AddRequest(ushort requestId)
    {
        var completion = new TaskCompletionSource<DnsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(requestId, completion))
            throw new DnsClientException("Request ID collision");
        return completion;
    }

    private void RemoveRequest(ushort requestId, TaskCompletionSource<DnsResponse> completion)
    {
        _pendingRequests.TryRemove(new(requestId, completion));
    }

    private void CompleteRequest(DnsResponse response)
    {
        if (_pendingRequests.TryGetValue(response.Id, out var completion))
            completion.TrySetResult(response);
    }
}