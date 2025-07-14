using JobService.Infrastructure;
using JobService.Services;
using JobService.Common;
using Microsoft.Extensions.Logging;

namespace JobService.Server;

public class Application
{
    private readonly WebApplication _app;
    private readonly ILogger<Application> _logger;
    private readonly GracefulShutdownService _shutdownService;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly IConnectionConfiguration _connectionConfiguration;

    public Application(WebApplicationBuilder builder, IConnectionConfiguration? connectionConfig = null)
    {
        _connectionConfiguration = CreateConnectionConfiguration(builder, connectionConfig);
        
        builder.Services.AddSingleton<IConnectionConfiguration>(_connectionConfiguration);
        ConfigureKestrel(builder, _connectionConfiguration);
        
        _app = builder.Build();
        
        _logger = _app.Services.GetRequiredService<ILogger<Application>>();
        _databaseInitializer = _app.Services.GetRequiredService<DatabaseInitializer>();
        
        var shutdownTokenSource = _app.Services.GetRequiredService<CancellationTokenSource>();
        var socketPath = GetSocketPath(_connectionConfiguration);
        _shutdownService = new GracefulShutdownService(_app.Services.GetRequiredService<ILogger<GracefulShutdownService>>(), shutdownTokenSource, socketPath);
    }
    
    private static IConnectionConfiguration CreateConnectionConfiguration(WebApplicationBuilder builder, IConnectionConfiguration? providedConfig)
    {
        if (providedConfig != null)
        {
            return providedConfig;
        }

        var defaultConfig = new ConnectionConfiguration();
        
        if (builder.Environment.EnvironmentName == "Testing")
        {
            var testPipeName = Environment.GetEnvironmentVariable("TEST_PIPE_NAME");
            var testSocketPath = Environment.GetEnvironmentVariable("TEST_SOCKET_PATH");

            if (!string.IsNullOrEmpty(testPipeName) || !string.IsNullOrEmpty(testSocketPath))
            {
                return new ConnectionConfiguration(
                    testPipeName ?? defaultConfig.PipeName,
                    testSocketPath ?? defaultConfig.SocketPath);
            }
        }
        
        return defaultConfig;
    }

    private static void ConfigureKestrel(WebApplicationBuilder builder, IConnectionConfiguration connectionConfig)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            ServerConfiguration.ConfigureKestrel(options, connectionConfig);
        });
    }

    private static string GetSocketPath(IConnectionConfiguration connectionConfig)
    {
        if (OperatingSystem.IsWindows())
        {
            return string.Empty;
        }
        
        return !string.IsNullOrEmpty(connectionConfig.SocketPath) 
            ? connectionConfig.SocketPath 
            : Path.Combine(Path.GetTempPath(), "jobservice.sock");
    }

    public async Task RunAsync()
    {
        try
        {
            _logger.LogInformation("JobService gRPC Server - Starting up");
            _logger.LogInformation("================================================");
            
            await InitializeDatabaseAsync();
            ConfigureGrpcServices();
            SetupGracefulShutdown();
            LogServerInfo();
            
            _logger.LogInformation("JobService gRPC Server - Ready to accept connections");
            _logger.LogInformation("================================================");
            
            await _app.RunAsync(_shutdownService.ShutdownToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Application shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing database...");
        await _databaseInitializer.InitializeAsync();
        _logger.LogInformation("Database initialization completed");
    }

    private void ConfigureGrpcServices()
    {
        _logger.LogInformation("Configuring gRPC services...");
        _app.MapGrpcService<JobGrpcService>();
        _logger.LogInformation("gRPC services configured");
    }

    private void SetupGracefulShutdown()
    {
        _logger.LogInformation("Setting up graceful shutdown handlers...");
        _shutdownService.SetupShutdownHandlers();
        _logger.LogInformation("Graceful shutdown handlers configured");
    }

    private void LogServerInfo()
    {
        ServerConfiguration.LogConnectionInfo(_app.Logger, _connectionConfiguration);
    }

    private async Task CleanupAsync()
    {
        _logger.LogInformation("Performing cleanup...");
        _shutdownService.CleanupResources();
        
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
        
        _logger.LogInformation("Cleanup completed");
    }
}