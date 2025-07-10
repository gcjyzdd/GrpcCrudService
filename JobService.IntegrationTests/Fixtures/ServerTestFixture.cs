using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobService.Common;
using System.Diagnostics;

namespace JobService.IntegrationTests.Fixtures;

public class ServerTestFixture : IAsyncDisposable
{
    private Process? _serverProcess;
    private readonly ConnectionConfiguration _connectionConfig;
    private readonly string _socketPath;

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

        var serverPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "JobService.Server"));
        
        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{serverPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Environment = 
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                    ["TEST_PIPE_NAME"] = _connectionConfig.PipeName,
                    ["TEST_SOCKET_PATH"] = _connectionConfig.SocketPath,
                    ["ConnectionStrings__DefaultConnection"] = $"Data Source=test_{Guid.NewGuid().ToString("N")[..8]}.db"
                }
            }
        };

        // Capture output for debugging
        _serverProcess.OutputDataReceived += (sender, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"Server: {e.Data}");
        };
        _serverProcess.ErrorDataReceived += (sender, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"Server Error: {e.Data}");
        };

        _serverProcess.Start();
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        
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
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                // Try graceful shutdown first
                _serverProcess.CloseMainWindow();
                
                // Wait up to 5 seconds for graceful shutdown
                if (!_serverProcess.WaitForExit(5000))
                {
                    // Force kill if graceful shutdown failed
                    _serverProcess.Kill();
                }
                
                await _serverProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping server: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _serverProcess?.Dispose();
        
        // Cleanup socket file if it exists
        if (!_connectionConfig.IsWindows && File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }
        
        GC.SuppressFinalize(this);
    }
}