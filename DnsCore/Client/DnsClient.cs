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
    private const int InitialRetryDelayMilliseconds = 500;
    private const int DefaultRequestTimeoutMilliseconds = 10000;

    private readonly TimeSpan _requestTimeout;
    private readonly DnsClientTransport[] _defaultTransports;
    private readonly DnsClientTransport[]? _tcpTransports;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<(DnsResponse Response, int TransportIndex)>> _pendingRequests = [];
    private readonly CancellationTokenSource _receiveTaskCancellation = new();
    private readonly Task[] _receiveTasks;

    public DnsClient(DnsTransportType transportType, EndPoint[] serverEndPoints, TimeSpan requestTimeout)
    {
        _requestTimeout = requestTimeout;
        _defaultTransports = new DnsClientTransport[serverEndPoints.Length];
        if (transportType == DnsTransportType.All)
            _tcpTransports = new DnsClientTransport[serverEndPoints.Length];
        _receiveTasks = new Task[serverEndPoints.Length];
        for (var i = 0; i < serverEndPoints.Length; ++i)
        {
            _defaultTransports[i] = DnsClientTransport.Create(transportType, serverEndPoints[i]);
            if (_tcpTransports is not null)
                _tcpTransports[i] = DnsClientTransport.Create(DnsTransportType.TCP, serverEndPoints[i]);
            _receiveTasks[i] = ReceiveResponses(i, _receiveTaskCancellation.Token);
        }
    }

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint) : this(transportType, [serverEndPoint], TimeSpan.FromMilliseconds(DefaultRequestTimeoutMilliseconds)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port, TimeSpan requestTimeout) : this(transportType, [new IPEndPoint(serverAddress, port)], requestTimeout) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port = DnsDefaults.Port) : this(transportType, new IPEndPoint(serverAddress, port)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, TimeSpan requestTimeout) : this(transportType, serverAddress, DnsDefaults.Port, requestTimeout) {}

    public async ValueTask DisposeAsync()
    {
        await _receiveTaskCancellation.CancelAsync().ConfigureAwait(false);
        await Task.WhenAll(_receiveTasks).ConfigureAwait(false);
        _receiveTaskCancellation.Dispose();
        foreach (var transport in _defaultTransports)
            await transport.DisposeAsync().ConfigureAwait(false);
        if (_tcpTransports is not null)
            foreach (var transport in _tcpTransports)
                await transport.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask<DnsResponse> Query(DnsRequest request, CancellationToken cancellationToken = default)
    {
        using var timeoutCancellation = new CancellationTokenSource(_requestTimeout);
        using var aggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
        var responseCompletion = AddRequest(request.Id);
        try
        {
            await using var cancellationRegistration = aggregatedCancellation.Token.Register(() => responseCompletion.TrySetCanceled()).ConfigureAwait(false);
            var singleRequestDelay = TimeSpan.FromMicroseconds(InitialRetryDelayMilliseconds);
            var iteration = 0;
            while (true)
            {
                using var singleRequestCancellation = new CancellationTokenSource(singleRequestDelay);
                using var singleRequestAggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(singleRequestCancellation.Token, aggregatedCancellation.Token);
                try
                {
                    await SendRequest(iteration++, request, singleRequestAggregatedCancellation.Token).ConfigureAwait(false);
                    var (response, transportIndex) = await responseCompletion.Task.WaitAsync(singleRequestAggregatedCancellation.Token).ConfigureAwait(false);
                    if (!response.Truncated || _tcpTransports is null)
                        return response;
                    
                    var tcpTransport = _tcpTransports[transportIndex];
                    await SendRequest(tcpTransport, request, aggregatedCancellation.Token).ConfigureAwait(false);
                    return await ReceiveResponse(tcpTransport, aggregatedCancellation.Token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (e is OperationCanceledException && singleRequestCancellation.IsCancellationRequested ||
                        e is DnsClientException)
                    {
                        singleRequestDelay *= 2;
                        continue;
                    }
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
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

    private async ValueTask SendRequest(int iteration, DnsRequest request, CancellationToken cancellationToken)
    {
        await SendRequest(_defaultTransports[iteration % _defaultTransports.Length], request, cancellationToken).ConfigureAwait(false);
    }
    
    private static async ValueTask SendRequest(DnsClientTransport transport, DnsRequest request, CancellationToken cancellationToken)
    {
        using var buffer = new DnsTransportBuffer(DnsDefaults.MaxUdpMessageSize);
        var len = DnsRequestEncoder.Encode(buffer.Span, request);
        buffer.Resize(len);
        using var requestMessage = new DnsTransportMessage(buffer);
        await transport.Send(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<DnsResponse> ReceiveResponse(DnsClientTransport transport, CancellationToken cancellationToken)
    {
        using var responseMessage = await transport.Receive(cancellationToken).ConfigureAwait(false);
        return DnsResponseEncoder.Decode(responseMessage.Buffer.Span);
    }

    private async Task ReceiveResponses(int transportIndex, CancellationToken cancellationToken)
    {
        var transport  = _defaultTransports[transportIndex];
        while (true)
        {
            var response = await ReceiveResponse(transport, cancellationToken).ConfigureAwait(false);
            CompleteRequest(response, transportIndex);
        }
    }

    private TaskCompletionSource<(DnsResponse Response, int TransportIndex)> AddRequest(ushort requestId)
    {
        var completion = new TaskCompletionSource<(DnsResponse Response, int TransportIndex)>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(requestId, completion))
            throw new DnsClientException("Request ID collision");
        return completion;
    }

    private void RemoveRequest(ushort requestId, TaskCompletionSource<(DnsResponse Response, int TransportIndex)> completion)
    {
        _pendingRequests.TryRemove(new(requestId, completion));
    }

    private void CompleteRequest(DnsResponse response, int transportIndex)
    {
        if (_pendingRequests.TryGetValue(response.Id, out var completion))
            completion.TrySetResult((response, transportIndex));
    }
}