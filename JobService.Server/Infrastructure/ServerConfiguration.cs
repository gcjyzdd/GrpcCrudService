using Microsoft.AspNetCore.Server.Kestrel.Core;
using JobService.Common;

namespace JobService.Infrastructure;

public class ServerConfiguration
{
    public static string ConfigureKestrel(KestrelServerOptions options, IConnectionConfiguration connectionConfig)
    {
        var socketPath = string.Empty;

        if (connectionConfig.IsWindows)
        {
            options.ListenNamedPipe(connectionConfig.PipeName);
        }
        else
        {
            socketPath = connectionConfig.SocketPath;
            CleanupExistingSocket(socketPath);
            options.ListenUnixSocket(socketPath);
        }

        return socketPath;
    }

    private static void CleanupExistingSocket(string socketPath)
    {
        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }
    }

    public static void LogConnectionInfo(ILogger logger, IConnectionConfiguration connectionConfig)
    {
        if (connectionConfig.IsWindows)
        {
            logger.LogInformation("gRPC Server listening on named pipe: {PipeName}", connectionConfig.PipeName);
        }
        else
        {
            logger.LogInformation("gRPC Server listening on Unix socket: {SocketPath}", connectionConfig.SocketPath);
        }
    }
}