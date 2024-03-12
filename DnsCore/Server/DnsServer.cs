using DnsCore.Model;
using System.Buffers;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

using DnsCore.Server.Transport;

namespace DnsCore.Server;

public sealed partial class DnsServer : IDisposable, IAsyncDisposable
{
    private readonly EndPoint _endPoint;
    private readonly DnsTransportType _transportType;
    private readonly Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> _handler;
    private readonly ILogger? _logger;
    private bool _started;
    private bool _disposed;
    private Task? _runTask;
    private CancellationTokenSource? _runCancellation;

    public DnsServer(EndPoint endPoint, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(handler);

        _endPoint = endPoint;
        _transportType = transportType;
        _handler = handler;
        _logger = logger;
    }

    public DnsServer(IPAddress address, ushort port, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, port), transportType, handler, logger)
    {
    }

    public DnsServer(IPAddress address, DnsTransportType transportType, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, DnsDefaults.Port), transportType, handler, logger)
    {
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
            throw new InvalidOperationException("Server is already running");

        _runCancellation = new CancellationTokenSource();
        _runTask = Run(_runCancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_runCancellation is not null)
            await _runCancellation.CancelAsync().ConfigureAwait(false);
        if (_runTask is not null)
            try
            {
                await _runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        _runCancellation?.Dispose();
    }

    public void Dispose() => DisposeAsync().AsTask().Wait();

    public async Task Run(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
            throw new InvalidOperationException("Server is already running");
        _started = true;
        try
        {
            using var transport = DnsServerTransport.Create(_endPoint, _transportType);
            if (_logger is not null)
                LogStartedDnsServer(_logger, _endPoint);
            var tasks = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
#pragma warning disable CA2016 // On purpose
            await tasks.Writer.WriteAsync(AcceptConnections(tasks, transport, cancellationToken));
#pragma warning restore CA2016
            await WaitForTasksToCompleteOrFail(tasks.Reader);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorRunningDnsServer(_logger, e, _endPoint);
        }
        finally
        {
            if (_logger is not null)
                LogStoppedDnsServer(_logger, _endPoint);
        }
    }

    private static async Task WaitForTasksToCompleteOrFail(ChannelReader<Task> tasksReader)
    {
        var waitForMoreTasks = tasksReader.WaitToReadAsync().AsTask();
        List<Task> pendingTasks = [waitForMoreTasks];
        while (pendingTasks.Count > 0)
        {
            var task = await Task.WhenAny(pendingTasks);
            if (task.IsFaulted)
                await task; // We want to throw the exception

            pendingTasks.Remove(task);
            if (task != waitForMoreTasks)
                continue;

            if (!await waitForMoreTasks)
                continue;

            while (tasksReader.TryRead(out var newTask))
                pendingTasks.Add(newTask);

            waitForMoreTasks = tasksReader.WaitToReadAsync().AsTask();
            pendingTasks.Add(waitForMoreTasks);
        }
    }

    private async Task AcceptConnections(ChannelWriter<Task> tasks, DnsServerTransport serverTransport, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                try
                {
                    var connection = await serverTransport.Accept(cancellationToken).ConfigureAwait(false);
    #pragma warning disable CA2016 // On purpose
                    await tasks.WriteAsync(ProcessRequests(serverTransport, connection, cancellationToken));
    #pragma warning restore CA2016
                }
                catch (DnsServerTransportException e)
                {
                    if (_logger is not null)
                        LogErrorReceivingDnsRequest(_logger, e);
                }
            }
        }
        finally
        {
            tasks.Complete();
        }
    }

    private async Task ProcessRequests(DnsServerTransport serverTransport, DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        using (connection)
        {
            while (true)
            {
                DnsServerTransportRequest? transportRequest;
                try
                {
                    transportRequest = await connection.Receive(cancellationToken).ConfigureAwait(false);
                }
                catch (DnsServerTransportException e)
                {
                    if (_logger is not null)
                        LogErrorReceivingDnsRequest(_logger, e);
                    continue;
                }

                if (transportRequest is null)
                    break;

                DnsRequest request;
                try
                {
                    using (transportRequest)
                        request = DnsRequest.Decode(transportRequest.Buffer);
                }
                catch (FormatException e)
                {
                    if (_logger != null)
                        LogErrorDecodingDnsRequest(_logger, e, connection.RemoteEndPoint);
                    continue;
                }
                await HandleRequest(serverTransport, connection, request, cancellationToken);
            }
        }
    }

    private async Task HandleRequest(DnsServerTransport serverTransport, DnsServerTransportConnection connection, DnsRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var response = await InvokeRequestHandler(connection, request, cancellationToken).ConfigureAwait(false);
        var buffer = ArrayPool<byte>.Shared.Rent(serverTransport.MaxMessageSize);
        try
        {
            var length = EncodeResponse(connection, request, response, buffer);
            await connection.Send(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        }
        catch (DnsServerTransportException e)
        {
            if (_logger is not null)
                LogErrorSendingDnsResponse(_logger, e, connection.RemoteEndPoint, response);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async ValueTask<DnsResponse> InvokeRequestHandler(DnsServerTransportConnection connection, DnsRequest request, CancellationToken cancellationToken)
    {
        if (_logger is not null)
            LogDnsRequest(_logger, connection.RemoteEndPoint, request);
        DnsResponse response;
        try
        {
            response = await _handler(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorHandlingDnsRequest(_logger, e, connection.RemoteEndPoint, request);
            response = request.Reply();
            response.Status = DnsResponseStatus.ServerFailure;
        }
        if (_logger is not null)
            LogDnsResponse(_logger, connection.RemoteEndPoint, response);
        return response;
    }

    private int EncodeResponse(DnsServerTransportConnection connection, DnsRequest request, DnsResponse response, Span<byte> buffer)
    {
        try
        {
            return response.Encode(buffer);
        }
        catch (FormatException e)
        {
            if (_logger is not null)
                LogErrorDnsResponseTruncated(_logger, e, connection.RemoteEndPoint, response);

            response = request.Reply();
            response.Truncated = true;
            return response.Encode(buffer);
        }
    }

    #region Logging

    [LoggerMessage(LogLevel.Debug, "Started DNS server on {EndPoint}")]
    private static partial void LogStartedDnsServer(ILogger logger, EndPoint endPoint);

    [LoggerMessage(LogLevel.Debug, "Stopped DNS server on {EndPoint}")]
    private static partial void LogStoppedDnsServer(ILogger logger, EndPoint endPoint);

    [LoggerMessage(LogLevel.Error, "Error receiving DNS request")]
    private static partial void LogErrorReceivingDnsRequest(ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Error, "Error sending DNS response to {RemoteEndPoint}:\n{Response}")]
    private static partial void LogErrorSendingDnsResponse(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Error, "Error decoding DNS request from {RemoteEndPoint}")]
    private static partial void LogErrorDecodingDnsRequest(ILogger logger, Exception e, EndPoint remoteEndPoint);

    [LoggerMessage(LogLevel.Error, "Truncated DNS response to {RemoteEndPoint}:\n{Response}")]
    private static partial void LogErrorDnsResponseTruncated(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error running DNS server on {EndPoint}")]
    private static partial void LogErrorRunningDnsServer(ILogger logger, Exception e, EndPoint endPoint);

    [LoggerMessage(LogLevel.Debug, "Received DNS request from {RemoteEndPoint}:\n{Request}")]
    private static partial void LogDnsRequest(ILogger logger, EndPoint remoteEndPoint, DnsRequest request);

    [LoggerMessage(LogLevel.Debug, "Sending DNS response to {RemoteEndPoint}:\n{Response}")]
    private static partial void LogDnsResponse(ILogger logger, EndPoint remoteEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error handling DNS request from {RemoteEndPoint}:\n{Request}")]
    private static partial void LogErrorHandlingDnsRequest(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsRequest request);

    #endregion
}