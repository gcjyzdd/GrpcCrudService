using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace JobService.Infrastructure;

public class ServerConfiguration
{
    public static string ConfigureKestrel(KestrelServerOptions options)
    {
        var socketPath = string.Empty;

        if (OperatingSystem.IsWindows())
        {
            options.ListenNamedPipe("JobServicePipe");
        }
        else
        {
            socketPath = GetUnixSocketPath();
            CleanupExistingSocket(socketPath);
            options.ListenUnixSocket(socketPath);
        }

        return socketPath;
    }

    private static string GetUnixSocketPath()
    {
        return Path.Combine(Path.GetTempPath(), "jobservice.sock");
    }

    private static void CleanupExistingSocket(string socketPath)
    {
        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }
    }

    public static void LogConnectionInfo(ILogger logger, string socketPath)
    {
        if (OperatingSystem.IsWindows())
        {
            logger.LogInformation("gRPC Server listening on named pipe: JobServicePipe");
        }
        else
        {
            logger.LogInformation("gRPC Server listening on Unix socket: {SocketPath}", socketPath);
        }
    }
}