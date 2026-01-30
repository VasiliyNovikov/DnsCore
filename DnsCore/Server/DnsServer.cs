using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DnsCore.Common;
using DnsCore.Model;
using DnsCore.Model.Encoding;
using DnsCore.Server.Transport;
using DnsCore.Utils;

using Microsoft.Extensions.Logging;

namespace DnsCore.Server;

public sealed partial class DnsServer
{
    private readonly Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> _handler;
    private readonly DnsServerOptions _options;
    private readonly ILogger? _logger;

    public DnsServer(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, DnsServerOptions? options = null, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _options = options ?? new DnsServerOptions();
        _options.Validate();
        _logger = logger;
    }

    public DnsServer(Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null) : this(handler, null, logger) { }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (_logger is not null)
            LogStartedDnsServer(_logger, _options.TransportType);
        try
        {
            await ServerTaskScheduler.Run(async (scheduler, ct) =>
            {
                foreach (var transport in DnsServerTransport.Create(_options.TransportType, _options.EndPoints))
                    await scheduler.Enqueue(async (s, ct) => await AcceptConnections(s, transport, ct).ConfigureAwait(false)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorRunningDnsServer(_logger, e, _options.TransportType);
        }
        finally
        {
            if (_logger is not null)
                LogStoppedDnsServer(_logger, _options.TransportType);
        }
    }

    private async Task AcceptConnections(ServerTaskScheduler scheduler, DnsServerTransport serverTransport, CancellationToken cancellationToken)
    {
        await using (serverTransport.ConfigureAwait(false))
        {
            if (_logger is not null)
                LogAcceptingConnections(_logger, serverTransport.EndPoint, serverTransport.Type);

            var retryInterval = TimeSpan.Zero;
            while (true)
            {
                try
                {
                    var connection = await serverTransport.Accept(cancellationToken).ConfigureAwait(false);
                    retryInterval = TimeSpan.Zero;
                    await scheduler.Enqueue(async (_, ct) => await ProcessRequests(connection, ct).ConfigureAwait(false)).ConfigureAwait(false);
                }
                catch (DnsServerTransportException e)
                {
                    if (_logger is not null)
                        LogErrorReceivingDnsRequest(_logger, _options.TransportErrorLogLevel, e);

                    if (!e.IsTransient)
                        throw;

                    if (retryInterval == TimeSpan.Zero)
                        retryInterval = _options.AcceptRetryInitialInterval;
                    else
                    {
                        retryInterval *= 2;
                        if (retryInterval > _options.AcceptRetryMaxInterval)
                            throw;
                    }

                    await Task.Delay(retryInterval, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task ProcessRequests(DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        using (connection)
        {
            while (true)
            {
                if (await ReceiveRequest(connection, cancellationToken).ConfigureAwait(false) is not { } request)
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
                LogErrorReceivingDnsRequest(_logger, _options.TransportErrorLogLevel, e);
            return null;
        }
    }

    private async ValueTask<DnsRequest?> ReceiveRequest(DnsServerTransportConnection connection, CancellationToken cancellationToken)
    {
        if (await ReceiveTransportRequest(connection, cancellationToken).ConfigureAwait(false) is not { } transportRequest)
            return null;

        try
        {
            using (transportRequest)
                return DnsRequestEncoder.Decode(transportRequest.Buffer.Span);
        }
        catch (FormatException e)
        {
            if (_logger is not null)
                LogErrorDecodingDnsRequest(_logger, _options.DecodingErrorLogLevel, e, connection.RemoteEndPoint, connection.TransportType);
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
                            LogErrorDnsResponseTruncated(_logger, _options.ResponseTruncationLogLevel, connection.RemoteEndPoint, connection.TransportType, response);

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
                LogErrorSendingDnsResponse(_logger, _options.TransportErrorLogLevel, e, connection.RemoteEndPoint, connection.TransportType, response);
        }
    }

    #region Logging

    [LoggerMessage(LogLevel.Debug, "Started DNS server on {TransportType}")]
    private static partial void LogStartedDnsServer(ILogger logger, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Debug, "Stopped DNS server on {TransportType}")]
    private static partial void LogStoppedDnsServer(ILogger logger, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Debug, "Accepting connections on {EndPoint} {TransportType}")]
    private static partial void LogAcceptingConnections(ILogger logger, EndPoint endPoint, DnsTransportType transportType);

    [LoggerMessage("Error receiving DNS request")]
    private static partial void LogErrorReceivingDnsRequest(ILogger logger, LogLevel logLevel, Exception e);

    [LoggerMessage("Error sending DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogErrorSendingDnsResponse(ILogger logger, LogLevel logLevel, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage("Error decoding DNS request from {RemoteEndPoint} {TransportType}")]
    private static partial void LogErrorDecodingDnsRequest(ILogger logger, LogLevel logLevel, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType);

    [LoggerMessage("Truncated DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogErrorDnsResponseTruncated(ILogger logger, LogLevel logLevel, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error running DNS server on {TransportType}")]
    private static partial void LogErrorRunningDnsServer(ILogger logger, Exception e, DnsTransportType transportType);

    [LoggerMessage(LogLevel.Debug, "Received DNS request from {RemoteEndPoint} {TransportType}:\n{Request}")]
    private static partial void LogDnsRequest(ILogger logger, EndPoint remoteEndPoint, DnsTransportType transportType, DnsRequest request);

    [LoggerMessage(LogLevel.Debug, "Sending DNS response to {RemoteEndPoint} {TransportType}:\n{Response}")]
    private static partial void LogDnsResponse(ILogger logger, EndPoint remoteEndPoint, DnsTransportType transportType, DnsResponse response);

    [LoggerMessage(LogLevel.Critical, "Error handling DNS request from {RemoteEndPoint} {TransportType}:\n{Request}")]
    private static partial void LogErrorHandlingDnsRequest(ILogger logger, Exception e, EndPoint remoteEndPoint, DnsTransportType transportType, DnsRequest request);

    #endregion
}