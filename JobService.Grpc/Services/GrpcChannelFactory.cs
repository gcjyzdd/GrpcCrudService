using Grpc.Net.Client;
using JobService.Grpc.Interfaces;
using JobService.Common;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Net.Sockets;

namespace JobService.Grpc.Services;

public class GrpcChannelFactory : IGrpcChannelFactory
{
    private readonly ILogger<GrpcChannelFactory> _logger;
    private readonly IConnectionConfiguration _connectionConfig;

    public GrpcChannelFactory(ILogger<GrpcChannelFactory> logger, IConnectionConfiguration connectionConfig)
    {
        _logger = logger;
        _connectionConfig = connectionConfig;
    }

    public GrpcChannel CreateChannel()
    {
        var connectionString = "http://localhost";

        if (_connectionConfig.IsWindows)
        {
            _logger.LogInformation("Creating Windows named pipe connection to: {PipeName}", _connectionConfig.PipeName);
            return CreateWindowsNamedPipeChannel(connectionString);
        }
        else
        {
            _logger.LogInformation("Creating Unix domain socket connection to: {SocketPath}", _connectionConfig.SocketPath);
            return CreateUnixDomainSocketChannel(connectionString, _connectionConfig.SocketPath);
        }
    }

    private GrpcChannel CreateWindowsNamedPipeChannel(string connectionString)
    {
        return GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler()
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    try
                    {
                        var pipeClient = new NamedPipeClientStream(".", _connectionConfig.PipeName, PipeDirection.InOut);

                        // Add timeout to prevent indefinite hanging
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                        await pipeClient.ConnectAsync(combinedCts.Token);
                        _logger.LogDebug("Successfully connected to named pipe");
                        return pipeClient;
                    }
                    catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "Named pipe connection was cancelled");
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TimeoutException($"Failed to connect to named pipe '{_connectionConfig.PipeName}' within 10 seconds");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to named pipe");
                        throw;
                    }
                }
            }
        });
    }

    private GrpcChannel CreateUnixDomainSocketChannel(string connectionString, string socketPath)
    {
        return GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler()
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    try
                    {
                        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        var endpoint = new UnixDomainSocketEndPoint(socketPath);
                        await socket.ConnectAsync(endpoint, cancellationToken);
                        _logger.LogDebug("Successfully connected to Unix domain socket");
                        return new NetworkStream(socket, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to Unix domain socket: {SocketPath}", socketPath);
                        throw;
                    }
                }
            }
        });
    }
}