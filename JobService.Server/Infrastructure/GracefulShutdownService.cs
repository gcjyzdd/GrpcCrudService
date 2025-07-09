using System.Runtime.InteropServices;

namespace JobService.Infrastructure;

public class GracefulShutdownService
{
    private readonly ILogger<GracefulShutdownService> _logger;
    private readonly CancellationTokenSource _shutdownTokenSource;
    private readonly string _socketPath;
    private volatile bool _shutdownInitiated = false;

    public GracefulShutdownService(ILogger<GracefulShutdownService> logger, CancellationTokenSource shutdownTokenSource, string socketPath)
    {
        _logger = logger;
        _shutdownTokenSource = shutdownTokenSource;
        _socketPath = socketPath;
    }

    public CancellationToken ShutdownToken => _shutdownTokenSource.Token;

    public void SetupShutdownHandlers()
    {
        SetupConsoleHandlers();
        SetupProcessExitHandlers();
        SetupWindowsSpecificHandlers();
    }

    private void SetupConsoleHandlers()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            InitiateShutdown("Ctrl+C received");
        };
    }

    private void SetupProcessExitHandlers()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            InitiateShutdown("Process exit event received");
        };
    }

    private void SetupWindowsSpecificHandlers()
    {
        if (!OperatingSystem.IsWindows()) return;

        SetConsoleCtrlHandler(eventType =>
        {
            if (IsShutdownEvent(eventType))
            {
                InitiateShutdown($"Windows console event received ({eventType})");
                
                // Give the application time to shut down gracefully
                Thread.Sleep(5000);
                return true;
            }
            return false;
        }, true);
    }

    private static bool IsShutdownEvent(CtrlType eventType)
    {
        return eventType == CtrlType.CTRL_C_EVENT ||
               eventType == CtrlType.CTRL_BREAK_EVENT ||
               eventType == CtrlType.CTRL_CLOSE_EVENT ||
               eventType == CtrlType.CTRL_LOGOFF_EVENT ||
               eventType == CtrlType.CTRL_SHUTDOWN_EVENT;
    }

    private void InitiateShutdown(string reason)
    {
        if (_shutdownInitiated) return;
        
        lock (this)
        {
            if (_shutdownInitiated) return;
            _shutdownInitiated = true;
        }

        _logger.LogInformation("{Reason}, initiating graceful shutdown...", reason);
        
        try
        {
            if (!_shutdownTokenSource.IsCancellationRequested)
            {
                _shutdownTokenSource.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // Token source already disposed, shutdown already in progress
            _logger.LogDebug("CancellationTokenSource already disposed during shutdown.");
        }
    }

    public void CleanupResources()
    {
        _logger.LogInformation("Cleaning up resources...");
        
        CleanupUnixSocket();
        
        _logger.LogInformation("Graceful shutdown completed.");
    }

    private void CleanupUnixSocket()
    {
        if (string.IsNullOrEmpty(_socketPath) || !File.Exists(_socketPath)) return;

        try
        {
            File.Delete(_socketPath);
            _logger.LogInformation("Unix socket file deleted: {SocketPath}", _socketPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Unix socket file: {SocketPath}", _socketPath);
        }
    }

    // Windows console event handler
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

    private delegate bool ConsoleCtrlDelegate(CtrlType ctrlType);

    private enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }
}