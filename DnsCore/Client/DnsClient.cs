using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Client.Transport;
using DnsCore.Common;
using DnsCore.Model;

namespace DnsCore.Client;

public sealed class DnsClient : IAsyncDisposable
{
    private readonly TimeSpan _requestTimeout;
    private readonly DnsClientTransport _transport;
    private readonly Dictionary<ushort, TaskCompletionSource<DnsResponse>> _pendingRequests = [];
    private readonly ReaderWriterLockSlim _pendingRequestsLock = new();
    private readonly CancellationTokenSource _receiveTaskCancellation = new();
    private readonly Task _receiveTask;

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint, TimeSpan requestTimeout)
    {
        _requestTimeout = requestTimeout;
        _transport = DnsClientTransport.Create(transportType, serverEndPoint);
        _receiveTask = ReceiveResponses(_receiveTaskCancellation.Token);
    }

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint) : this(transportType, serverEndPoint, TimeSpan.FromSeconds(5)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port, TimeSpan requestTimeout) : this(transportType, new IPEndPoint(serverAddress, port), requestTimeout) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port) : this(transportType, new IPEndPoint(serverAddress, port)) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, TimeSpan requestTimeout) : this(transportType, serverAddress, DnsDefaults.Port, requestTimeout) {}
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress) : this(transportType, serverAddress, DnsDefaults.Port) {}

    public async ValueTask DisposeAsync()
    {
        await _receiveTaskCancellation.CancelAsync();
        await _receiveTask;
        _receiveTaskCancellation.Dispose();
        await _transport.DisposeAsync();
        _pendingRequestsLock.Dispose();
    }

    public async ValueTask<DnsResponse> Query(DnsRequest request, CancellationToken cancellationToken = default)
    {
        var responseCompletion = AddRequest(request.Id);
        using var timeoutCancellation = new CancellationTokenSource(_requestTimeout);
        using var aggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
        await using var cancellationRegistration = aggregatedCancellation.Token.Register(() => responseCompletion.TrySetCanceled()).ConfigureAwait(false);
        try
        {
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
        var buffer = DnsBufferPool.Rent(DnsDefaults.MaxUdpMessageSize);
        try
        {
            var len = request.Encode(buffer);
            using var requestMessage = new DnsTransportMessage(buffer, len, false);
            await _transport.Send(requestMessage, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            DnsBufferPool.Return(buffer);
        }
    }

    private async Task ReceiveResponses(CancellationToken cancellationToken)
    {
        await Task.Yield();
        while (true)
        {
            using var responseMessage = await _transport.Receive(cancellationToken).ConfigureAwait(false);
            CompleteRequest(DnsResponse.Decode(responseMessage.Buffer.Span));
        }
    }

    private TaskCompletionSource<DnsResponse> AddRequest(ushort requestId)
    {
        _pendingRequestsLock.EnterWriteLock();
        try
        {
            var completion = new TaskCompletionSource<DnsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[requestId] = completion;
            return completion;
        }
        finally
        {
            _pendingRequestsLock.ExitWriteLock();
        }
    }

    private void RemoveRequest(ushort requestId, TaskCompletionSource<DnsResponse> completion)
    {
        _pendingRequestsLock.EnterWriteLock();
        try
        {
            if (_pendingRequests.TryGetValue(requestId, out var current) && current == completion)
                _pendingRequests.Remove(requestId);
        }
        finally
        {
            _pendingRequestsLock.ExitWriteLock();
        }
    }

    private void CompleteRequest(DnsResponse response)
    {
        _pendingRequestsLock.EnterReadLock();
        try
        {
            if (_pendingRequests.TryGetValue(response.Id, out var completion))
                completion.TrySetResult(response);
        }
        finally
        {
            _pendingRequestsLock.ExitReadLock();
        }
    }
}