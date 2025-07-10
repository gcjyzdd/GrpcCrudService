using Grpc.Net.Client;
using JobService.Client.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Net.Sockets;

namespace JobService.Client.Services;

public class GrpcChannelFactory : IGrpcChannelFactory
{
    private readonly ILogger<GrpcChannelFactory> _logger;

    public GrpcChannelFactory(ILogger<GrpcChannelFactory> logger)
    {
        _logger = logger;
    }

    public GrpcChannel CreateChannel()
    {
        var connectionString = "http://localhost";
        
        if (OperatingSystem.IsWindows())
        {
            _logger.LogInformation("Creating Windows named pipe connection to: JobServicePipe");
            return CreateWindowsNamedPipeChannel(connectionString);
        }
        else
        {
            var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
            _logger.LogInformation("Creating Unix domain socket connection to: {SocketPath}", socketPath);
            return CreateUnixDomainSocketChannel(connectionString, socketPath);
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
                        var pipeClient = new NamedPipeClientStream(".", "JobServicePipe", PipeDirection.InOut);
                        await pipeClient.ConnectAsync(cancellationToken);
                        _logger.LogDebug("Successfully connected to named pipe");
                        return pipeClient;
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