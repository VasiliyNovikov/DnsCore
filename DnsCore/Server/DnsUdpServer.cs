using DnsCore.Model;
using System.Buffers;
using System.Net.Sockets;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

namespace DnsCore.Server;

public sealed partial class DnsUdpServer : IDisposable
{
    private const int MaxMessageSize = 512;
    private const ushort DefaultPort = 53;

    private readonly EndPoint _endPoint;
    private readonly Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> _handler;
    private readonly ILogger? _logger;
    private readonly Socket _serverSocket;
    private readonly Channel<Task> _ongoingRequests = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _serverTask;
    private readonly Task _ongoingRequestTask;

    public DnsUdpServer(EndPoint endPoint, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(handler);

        _endPoint = endPoint;
        _handler = handler;
        _logger = logger;

        _ongoingRequestTask = ProcessOngoingRequests();


        _serverSocket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _serverSocket.Bind(endPoint);
        _serverTask = Run();

        if (_logger is not null)
            LogStartedDnsServer(_logger, endPoint);
    }

    public DnsUdpServer(IPAddress address, ushort port, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, port), handler, logger)
    {
    }

    public DnsUdpServer(IPAddress address, Func<DnsRequest, CancellationToken, ValueTask<DnsResponse>> handler, ILogger? logger = null)
        : this(new IPEndPoint(address, DefaultPort), handler, logger)
    {
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _serverTask.Wait();
        _ongoingRequests.Writer.Complete();
        _ongoingRequestTask.Wait();
        _cancellation.Dispose();
        _serverSocket.Dispose();
    }

    private async Task Run()
    {
        var clientEndPoint = new IPEndPoint(_serverSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        var buffer = new byte[MaxMessageSize];
        try
        {
            while (true)
            {
                try
                {
                    var rawRequest = await _serverSocket.ReceiveFromAsync(buffer, SocketFlags.None, clientEndPoint, _cancellation.Token);
                    try
                    {
                        var message = DnsRequest.Decode(buffer.AsSpan(0, rawRequest.ReceivedBytes));
                        await _ongoingRequests.Writer.WriteAsync(HandleRequestCore(rawRequest.RemoteEndPoint, message));
                    }
                    catch (FormatException e)
                    {
                        if (_logger != null)
                            LogErrorParsingDnsRequest(_logger, e, rawRequest.RemoteEndPoint);
                    }
                }
                catch (SocketException e)
                {
                    if (_logger != null)
                        LogErrorReceivingDnsRequest(_logger, e);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (_logger is not null)
                LogErrorRunningDnsServer(_logger, e, _endPoint);
            throw;
        }
    }

    private async Task ProcessOngoingRequests()
    {
        await foreach (var request in _ongoingRequests.Reader.ReadAllAsync())
            await request;
    }

    private async Task HandleRequestCore(EndPoint clientEndPoint, DnsRequest request)
    {
        if (_logger is not null)
            LogDnsRequest(_logger, clientEndPoint, request);
        try
        {
            var response = await _handler(request, _cancellation.Token);
            if (_logger is not null)
                LogDnsResponse(_logger, clientEndPoint, response);
            var buffer = ArrayPool<byte>.Shared.Rent(MaxMessageSize);
            try
            {
                var length = response.Encode(buffer);
                await _serverSocket.SendToAsync(buffer.AsMemory(0, length), SocketFlags.None, clientEndPoint, _cancellation.Token);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (_logger is null)
                throw;
            LogErrorHandlingDnsRequest(_logger, e, clientEndPoint, request);
        }
    }

    #region Logging

    [LoggerMessage(LogLevel.Debug, "Started DNS server on {EndPoint}")]
    private static partial void LogStartedDnsServer(ILogger logger, EndPoint endPoint);

    [LoggerMessage(LogLevel.Error, "Error receiving DNS request")]
    private static partial void LogErrorReceivingDnsRequest(ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Error, "Error parsing DNS request from {ClientEndPoint}")]
    private static partial void LogErrorParsingDnsRequest(ILogger logger, Exception e, EndPoint clientEndPoint);

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