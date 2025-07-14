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

    public Application(WebApplicationBuilder builder)
    {
        var connectionConfig = ConfigureConnectionSettings(builder);
        ConfigureKestrel(builder, connectionConfig);
        
        _app = builder.Build();
        _connectionConfiguration = connectionConfig;
        
        _logger = _app.Services.GetRequiredService<ILogger<Application>>();
        _databaseInitializer = _app.Services.GetRequiredService<DatabaseInitializer>();
        
        var shutdownTokenSource = _app.Services.GetRequiredService<CancellationTokenSource>();
        var socketPath = GetSocketPath(connectionConfig);
        _shutdownService = new GracefulShutdownService(_app.Services.GetRequiredService<ILogger<GracefulShutdownService>>(), shutdownTokenSource, socketPath);
    }
    
    private static IConnectionConfiguration ConfigureConnectionSettings(WebApplicationBuilder builder)
    {
        var connectionConfig = new ConnectionConfiguration();

        if (builder.Environment.EnvironmentName == "Testing")
        {
            var testPipeName = Environment.GetEnvironmentVariable("TEST_PIPE_NAME");
            var testSocketPath = Environment.GetEnvironmentVariable("TEST_SOCKET_PATH");

            if (!string.IsNullOrEmpty(testPipeName))
                connectionConfig.PipeName = testPipeName;
            if (!string.IsNullOrEmpty(testSocketPath))
                connectionConfig.SocketPath = testSocketPath;
        }

        builder.Services.AddSingleton<IConnectionConfiguration>(connectionConfig);
        return connectionConfig;
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
            
            await InitializeDatabaseAsync();
            ConfigureGrpcServices();
            SetupGracefulShutdown();
            LogServerInfo();
            
            _logger.LogInformation("JobService gRPC Server - Ready to accept connections");
            
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