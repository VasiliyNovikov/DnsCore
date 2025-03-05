using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;
using DnsCore.Server.Transport;

using Microsoft.Extensions.Logging;

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
        if (_logger is not null)
            LogStartedDnsServer(_logger, _endPoint, _transportType);
        try
        {
            var tasks = new ServerTaskManager();
            await using (tasks.ConfigureAwait(false))
            {
                var transport = DnsServerTransport.Create(_transportType, [_endPoint]);
                await tasks.Add(AcceptConnections(tasks, transport, cancellationToken)).ConfigureAwait(false);

            }
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
                LogStoppedDnsServer(_logger, _endPoint, _transportType);
        }
    }

    private async Task AcceptConnections(ServerTaskManager tasks, DnsServerTransport serverTransport, CancellationToken cancellationToken)
    {
        try
        {
            await using (serverTransport.ConfigureAwait(false))
                while (true)
                {
                    try
                    {
                        var connection = await serverTransport.Accept(cancellationToken).ConfigureAwait(false);
                        await tasks.Add(ProcessRequests(connection, cancellationToken)).ConfigureAwait(false);
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
            tasks.CompleteAdding();
        }
    }

    private async Task ProcessRequests(DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        using (connection)
        {
            while (true)
            {
                var request = await ReceiveRequest(connection, cancellationToken).ConfigureAwait(false);
                if (request is null)
                    return;
                await HandleRequest(connection, request, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<DnsTransportMessage?> ReceiveTransportRequest(DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            return await connection.Receive(cancellationToken).ConfigureAwait(false);
        }
        catch (DnsServerTransportException e)
        {
            if (_logger is not null)
                LogErrorReceivingDnsRequest(_logger, e);
            return null;
        }
    }

    private async ValueTask<DnsRequest?> ReceiveRequest(DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        var transportRequest = await ReceiveTransportRequest(connection, cancellationToken).ConfigureAwait(false);
        if (transportRequest is null)
            return null;

        try
        {
            using (transportRequest)
                return DnsRequestEncoder.Decode(transportRequest.Buffer.Span);
        }
        catch (FormatException e)
        {
            if (_logger != null)
                LogErrorDecodingDnsRequest(_logger, e, connection.RemoteEndPoint, connection.TransportType);
            return null;
        }
    }

    private async Task HandleRequest(DnsServerTransportConnection connection, DnsRequest request, CancellationToken cancellationToken)
    {
        var response = await InvokeRequestHandler(connection, request, cancellationToken).ConfigureAwait(false);
        await SendResponse(connection, request, response, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<DnsResponse> InvokeRequestHandler(DnsServerTransportConnection connection, DnsRequest request, CancellationToken cancellationToken)
    {
        if (_logger is not null)
            LogDnsRequest(_logger, connection.RemoteEndPoint, connection.TransportType, request);
        DnsResponse response;
        try
        {
            response = await _handler(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorHandlingDnsRequest(_logger, e, connection.RemoteEndPoint, connection.TransportType, request);
            response = request.Reply();
            response.Status = DnsResponseStatus.ServerFailure;
        }
        if (_logger is not null)
            LogDnsResponse(_logger, connection.RemoteEndPoint, connection.TransportType, response);
        return response;
    }

    private DnsTransportMessage EncodeResponse(DnsServerTransportConnection connection, DnsRequest request, DnsResponse response)
    {
        var bufferSize = connection.DefaultMessageSize;
        using var buffer = new DnsTransportBuffer();
        while (true)
        {
            buffer.Resize(bufferSize);
            try
            {
                var length = DnsResponseEncoder.Encode(buffer.Span, response);
                buffer.Resize(length);
                return new DnsTransportMessage(buffer);
            }
            catch (FormatException) // Insufficient buffer size
            {
                var newBufferSize = (ushort)Math.Min(bufferSize * 2, connection.MaxMessageSize);
                if (newBufferSize == bufferSize) // Max size reached
                {
                    if (!response.Truncated)
                    {
                        if (_logger is not null)
                            LogErrorDnsResponseTruncated(_logger, connection.RemoteEndPoint, connection.TransportType, response);

                        response = request.Reply();
                        response.Truncated = true;
                    }
                    else
                        response.Questions.Clear();
                }
                else
                    bufferSize = newBufferSize;
            }
        }
    }

    private async ValueTask SendResponse(DnsServerTransportConnection connection, DnsRequest request, DnsResponse response, CancellationToken cancellationToken)
    {
        try
        {
            using var responseMessage = EncodeResponse(connection, request, response);
            await connection.Send(responseMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (DnsServerTransportException e)
        {
            if (_logger is not null)
                LogErrorSendingDnsResponse(_logger, e, connection.RemoteEndPoint, connection.TransportType, response);
        }
    }

    #region Logging

    [LoggerMessage(LogLevel.Debug, "Started DNS server on {EndPoint} {TransportType}")]
    private static partial void LogStartedDnsServer(ILogger logger, EndPoint endPoint, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Debug, "Stopped DNS server on {EndPoint} {TransportType}")]
    private static partial void LogStoppedDnsServer(ILogger logger, EndPoint endPoint, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Error, "Error receiving DNS request")]
    private static partial void LogErrorReceivingDnsRequest(ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Error, "Error sending DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogErrorSendingDnsResponse(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage(LogLevel.Error, "Error decoding DNS request from {RemoteEndPoint} {TransportType}")]
    private static partial void LogErrorDecodingDnsRequest(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Error, "Truncated DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogErrorDnsResponseTruncated(ILogger logger, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error running DNS server on {EndPoint}")]
    private static partial void LogErrorRunningDnsServer(ILogger logger, Exception e, EndPoint endPoint);

    [LoggerMessage(LogLevel.Debug, "Received DNS request from {RemoteEndPoint} {TransportType}:\n{Request}")]
    private static partial void LogDnsRequest(ILogger logger, EndPoint remoteEndPoint, DnsTransportType transportType, DnsRequest request);

    [LoggerMessage(LogLevel.Debug, "Sending DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogDnsResponse(ILogger logger, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error handling DNS request from {RemoteEndPoint} {TransportType}:\n{Request}")]
    private static partial void LogErrorHandlingDnsRequest(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType, DnsRequest request);

    #endregion
}