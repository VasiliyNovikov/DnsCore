using System;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Client.Resolver;
using DnsCore.Common;
using DnsCore.Model;

using RentedListCore;

namespace DnsCore.Client;

public sealed class DnsClient : IAsyncDisposable
{
    private readonly DnsClientOptions _options;
    private readonly DnsResolver[] _defaultResolvers;
    private readonly DnsResolver[]? _tcpResolvers;
    private int _transportIndex = -1;

    public DnsClient(DnsTransportType transportType, EndPoint[] serverEndPoints, DnsClientOptions? options = null)
    {
        _options = options ?? new();
        _defaultResolvers = new DnsResolver[serverEndPoints.Length];
        DnsTransportType defaultTransportType;
        if (transportType == DnsTransportType.All)
        {
            defaultTransportType = DnsTransportType.UDP;
            _tcpResolvers = new DnsResolver[serverEndPoints.Length];
        }
        else
            defaultTransportType = transportType;
        for (var i = 0; i < serverEndPoints.Length; ++i)
        {
            _defaultResolvers[i] = defaultTransportType == DnsTransportType.UDP
                ? new DnsUdpResolver(serverEndPoints[i], _options.Udp)
                : new DnsTcpResolver(serverEndPoints[i]);
            _tcpResolvers?[i] = new DnsTcpResolver(serverEndPoints[i]);
        }
    }

    public DnsClient(DnsTransportType transportType, IPAddress[] serverAddresses, DnsClientOptions? options = null)
        : this(transportType, serverAddresses.Select(a => new IPEndPoint(a, DnsDefaults.Port)).ToArray<EndPoint>(), options)
    {
    }

    public DnsClient(DnsTransportType transportType, EndPoint serverEndPoint, DnsClientOptions? options = null) : this(transportType, [serverEndPoint], options) { }
    public DnsClient(DnsTransportType transportType, IPAddress serverAddress, ushort port = DnsDefaults.Port, DnsClientOptions? options = null) : this(transportType, new IPEndPoint(serverAddress, port), options) { }
    public DnsClient(EndPoint[] serverEndPoints, DnsClientOptions? options = null) : this(DnsTransportType.All, serverEndPoints, options) { }
    public DnsClient(IPAddress[] serverAddresses, DnsClientOptions? options = null) : this(DnsTransportType.All, serverAddresses, options) { }
    public DnsClient(EndPoint serverEndPoint, DnsClientOptions? options = null) : this(DnsTransportType.All, serverEndPoint, options) { }
    public DnsClient(IPAddress serverAddress, ushort port = DnsDefaults.Port, DnsClientOptions? options = null) : this(DnsTransportType.All, serverAddress, port, options) { }
    public DnsClient(DnsClientOptions? options = null) : this(DnsTransportType.All, SystemDnsConfiguration.GetAddresses(), options) { }

    public async ValueTask DisposeAsync()
    {
        foreach (var resolver in _defaultResolvers)
            await resolver.DisposeAsync().ConfigureAwait(false);
        if (_tcpResolvers is not null)
            foreach (var resolver in _tcpResolvers)
                await resolver.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask<DnsResponse> Query(DnsName name, DnsRecordType type, CancellationToken cancellationToken = default)
    {
        return await Query(new DnsRequest(name, type), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<DnsResponse> Query(DnsRequest request, CancellationToken cancellationToken = default)
    {
        return await QueryWithTimeout(request, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<DnsResponse> QueryWithTimeout(DnsRequest request, CancellationToken cancellationToken)
    {
        using var timeoutCancellation = new CancellationTokenSource(_options.RequestTimeout);
        try
        {
            using var aggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
            return await QueryWithConcurrentRetries(request, aggregatedCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
        {
            throw new TimeoutException("DNS request timed out");
        }
    }

    private async ValueTask<DnsResponse> QueryWithConcurrentRetries(DnsRequest request, CancellationToken cancellationToken)
    {
        using RentedList<Task> tasks = new(2) { Task.CompletedTask };
        using RentedList<Task> unobservedTasks = [];
        using var completionCancellation = new CancellationTokenSource();
        try
        {
            var retryDelay = _options.InitialRetryDelay;
            var resolvers = _defaultResolvers;
            var failureRetryCount = 0;
            while (true)
            {
                tasks[0] = Task.Delay(retryDelay, cancellationToken);

                var resolver = resolvers[Interlocked.Increment(ref _transportIndex) % resolvers.Length];
                tasks.Add(resolver.Resolve(request, completionCancellation.Token).AsTask());
                var completedTask = await Task.WhenAny(tasks.Span).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (completedTask == tasks[0]) // Delay
                {
                    var jitter = Random.Shared.NextDouble() * 0.2 - 0.1; // ±10%
                    retryDelay = retryDelay * 2 * (1 + jitter);
                    continue;
                }

                try
                {
                    tasks.Remove(completedTask);

                    var response = await ((Task<DnsResponse>)completedTask).ConfigureAwait(false);
                    if (response.Truncated)
                    {
                        resolvers = _tcpResolvers ?? throw new DnsResponseTruncatedException();
                        if (tasks.Count > 1)
                        {
                            unobservedTasks.AddRange(tasks[1..]);
                            tasks.Clear(); // Clear all ongoing UDP requests
                            tasks.Add(Task.CompletedTask);
                        }
                        continue;
                    }

                    if (response.Status == DnsResponseStatus.Ok)
                        return response;

                    if (response.Status != DnsResponseStatus.ServerFailure || failureRetryCount++ == _options.FailureRetryCount)
                        throw new DnsResponseStatusException(response.Status);
                }
                catch (DnsSocketException)
                {
                    if (failureRetryCount++ == _options.FailureRetryCount)
                        throw;
                }
            }
        }
        finally
        {
            tasks.AddRange(unobservedTasks);
            await completionCancellation.CancelAsync().ConfigureAwait(false);
            using RentedList<Exception> exceptions = [];
            foreach (var task in tasks)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Ignore
                }
                catch (OperationCanceledException) when (completionCancellation.IsCancellationRequested)
                {
                    // Ignore
                }
                catch (DnsException)
                {
                    // Ignore
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            switch (exceptions.Count)
            {
                case 1:
                    ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
                    break;
                case > 1:
#pragma warning disable CA2219
                    throw new AggregateException(exceptions);
#pragma warning restore CA2219
            }
        }
    }
}