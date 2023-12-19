using DnsCore.Model;
using System.Buffers;
using System.Net.Sockets;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DnsCore.Server;

public sealed partial class DnsUdpServer : IDisposable, IAsyncDisposable
{
    private const int MaxMessageSize = 512;
    private const ushort DefaultPort = 53;

    private readonly EndPoint _endPoint;
    private readonly Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> _handler;
    private readonly ILogger? _logger;
    private bool _started;
    private bool _disposed;
    private Task? _runTask;
    private CancellationTokenSource? _runCancellation;

    public DnsUdpServer(EndPoint endPoint, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(handler);

        _endPoint = endPoint;
        _handler = handler;
        _logger = logger;
    }

    public DnsUdpServer(IPAddress address, ushort port, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, port), handler, logger)
    {
    }

    public DnsUdpServer(IPAddress address, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, DefaultPort), handler, logger)
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
            using var socket = new Socket(_endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(_endPoint);
            if (_logger is not null)
                LogStartedDnsServer(_logger, _endPoint);
            var tasks = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
#pragma warning disable CA2016 // On purpose
            await tasks.Writer.WriteAsync(ReceiveRequests(tasks, socket, cancellationToken));
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

    private static async Task WaitForTasksToCompleteOrFail(ChannelReader<Task> tasks)
    {
        List<Task> pendingTasks = new();
        while (await tasks.WaitToReadAsync())
        {
            while (tasks.TryRead(out var task))
                pendingTasks.Add(task);

            while (pendingTasks.Count > 0)
            {
                var task = await Task.WhenAny(pendingTasks);
                if (task.IsFaulted)
                    await task;
                pendingTasks.Remove(task);
            }
        }
    }

    private async Task ReceiveRequests(ChannelWriter<Task> tasks, Socket socket, CancellationToken cancellationToken)
    {
        try
        {
            await ReceiveRequestsCore(tasks, socket, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            tasks.Complete();
        }
    }

    private async Task ReceiveRequestsCore(ChannelWriter<Task> tasks, Socket socket, CancellationToken cancellationToken)
    {
        var clientEndPoint = new IPEndPoint(socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        var buffer = new byte[MaxMessageSize];
        while (true)
        {
            SocketReceiveFromResult rawRequest;
            try
            {
                rawRequest = await socket.ReceiveFromAsync(buffer, SocketFlags.None, clientEndPoint, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                if (_logger is not null)
                    LogErrorReceivingDnsRequest(_logger, e);
                continue;
            }

            DnsRequest request;
            try
            {
                request = DnsRequest.Decode(buffer.AsSpan(0, rawRequest.ReceivedBytes));
            }
            catch (FormatException e)
            {
                if (_logger != null)
                    LogErrorDecodingDnsRequest(_logger, e, rawRequest.RemoteEndPoint);
                continue;
            }
#pragma warning disable CA2016 // On purpose
            await tasks.WriteAsync(HandleRequest(socket, rawRequest.RemoteEndPoint, request, cancellationToken));
#pragma warning restore CA2016
        }
    }

    private async Task HandleRequest(Socket socket, EndPoint clientEndPoint, DnsRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var response = await InvokeRequestHandler(clientEndPoint, request, cancellationToken).ConfigureAwait(false);
        var buffer = ArrayPool<byte>.Shared.Rent(MaxMessageSize);
        try
        {
            int length = EncodeResponse(clientEndPoint, request, response, buffer);
            await socket.SendToAsync(buffer.AsMemory(0, length), SocketFlags.None, clientEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException e)
        {
            if (_logger is not null)
                LogErrorSendingDnsResponse(_logger, e, clientEndPoint, response);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async ValueTask<DnsResponse> InvokeRequestHandler(EndPoint clientEndPoint, DnsRequest request, CancellationToken cancellationToken)
    {
        if (_logger is not null)
            LogDnsRequest(_logger, clientEndPoint, request);
        DnsResponse response;
        try
        {
            response = await _handler(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorHandlingDnsRequest(_logger, e, clientEndPoint, request);
            response = request.Reply();
            response.Status = DnsResponseStatus.ServerFailure;
        }
        if (_logger is not null)
            LogDnsResponse(_logger, clientEndPoint, response);
        return response;
    }

    private int EncodeResponse(EndPoint clientEndPoint, DnsRequest request, DnsResponse response, Span<byte> buffer)
    {
        try
        {
            return response.Encode(buffer);
        }
        catch (FormatException e)
        {
            if (_logger is not null)
                LogErrorDnsResponseTruncated(_logger, e, clientEndPoint, response);

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

    [LoggerMessage(LogLevel.Error, "Error sending DNS response to {ClientEndPoint}:\n{Response}")]
    private static partial void LogErrorSendingDnsResponse(ILogger logger, Exception e, EndPoint clientEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Error, "Error decoding DNS request from {ClientEndPoint}")]
    private static partial void LogErrorDecodingDnsRequest(ILogger logger, Exception e, EndPoint clientEndPoint);

    [LoggerMessage(LogLevel.Error, "Truncated DNS response to {ClientEndPoint}:\n{Response}")]
    private static partial void LogErrorDnsResponseTruncated(ILogger logger, Exception e, EndPoint clientEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error running DNS server on {EndPoint}")]
    private static partial void LogErrorRunningDnsServer(ILogger logger, Exception e, EndPoint endPoint);

    [LoggerMessage(LogLevel.Debug, "Received DNS request from {ClientEndPoint}:\n{Request}")]
    private static partial void LogDnsRequest(ILogger logger, EndPoint clientEndPoint, DnsRequest request);

    [LoggerMessage(LogLevel.Debug, "Sending DNS response to {ClientEndPoint}:\n{Response}")]
    private static partial void LogDnsResponse(ILogger logger, EndPoint clientEndPoint, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error handling DNS request from {ClientEndPoint}:\n{Request}")]
    private static partial void LogErrorHandlingDnsRequest(ILogger logger, Exception e, EndPoint clientEndPoint, DnsRequest request);

    #endregion
}