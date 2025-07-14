using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobService.Common;
using JobService.Server;

namespace JobService.IntegrationTests.Fixtures;

public class ServerTestFixture : IAsyncDisposable
{
    private Application? _serverApplication;
    private readonly ConnectionConfiguration _connectionConfig;
    private readonly string _socketPath;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _runTask;

    public ConnectionConfiguration ConnectionConfig => _connectionConfig;

    public ServerTestFixture(ConnectionConfiguration connectionConfig)
    {
        _connectionConfig = connectionConfig;
        _socketPath = connectionConfig.SocketPath;
    }

    public async Task StartAsync()
    {
        // Clean up any existing socket file
        if (!_connectionConfig.IsWindows && File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }

        // Set environment variables for testing
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("TEST_PIPE_NAME", _connectionConfig.PipeName);
        Environment.SetEnvironmentVariable("TEST_SOCKET_PATH", _connectionConfig.SocketPath);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source=test_{Guid.NewGuid().ToString("N")[..8]}.db");

        // Create application using ApplicationFactory
        var args = new string[] { };
        _serverApplication = ApplicationFactory.CreateApplication(args, _connectionConfig);
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start the application in the background
        _runTask = Task.Run(async () =>
        {
            try
            {
                await _serverApplication.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
                throw;
            }
        });
        
        // Wait for server to start and create the socket/pipe
        var maxWait = TimeSpan.FromSeconds(10);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < maxWait)
        {
            if (_connectionConfig.IsWindows)
            {
                // For Windows named pipes, just wait a bit as they're created on demand
                await Task.Delay(2000);
                break;
            }
            else
            {
                // For Unix sockets, wait for the file to exist
                if (File.Exists(_socketPath))
                {
                    await Task.Delay(500); // Give it a moment to be ready
                    break;
                }
            }
            
            await Task.Delay(200);
        }
        
        if (!_connectionConfig.IsWindows && !File.Exists(_socketPath))
        {
            throw new InvalidOperationException($"Server failed to create socket at {_socketPath}");
        }
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_runTask != null)
        {
            try
            {
                await _runTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Server shutdown timed out");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during server shutdown: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        _cancellationTokenSource?.Dispose();
        
        // Cleanup socket file if it exists
        if (!_connectionConfig.IsWindows && File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }
        
        GC.SuppressFinalize(this);
    }
}