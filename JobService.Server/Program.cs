using JobService.Services;
using JobService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Configure graceful shutdown
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Create a cancellation token source for graceful shutdown
var shutdownTokenSource = new CancellationTokenSource();
var socketPath = string.Empty;

// Configure Kestrel to use named pipes
builder.WebHost.ConfigureKestrel(options =>
{
    if (OperatingSystem.IsWindows())
    {
        options.ListenNamedPipe("JobServicePipe");
    }
    else
    {
        // For Unix-like systems, use Unix domain sockets
        socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }
        options.ListenUnixSocket(socketPath);
    }
});

// Add services to the container.
builder.Services.AddGrpc();

// Add Entity Framework
builder.Services.AddDbContext<JobContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Setup graceful shutdown handling
SetupGracefulShutdown(app, shutdownTokenSource, socketPath);

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<JobContext>();
    try
    {
        context.Database.EnsureCreated();
        app.Logger.LogInformation("Database ensured created successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while ensuring the database was created.");
    }
}

// Configure the gRPC pipeline.
app.MapGrpcService<JobService.Services.JobGrpcService>();

// Log connection information
if (OperatingSystem.IsWindows())
{
    app.Logger.LogInformation("gRPC Server listening on named pipe: JobServicePipe");
}
else
{
    app.Logger.LogInformation("gRPC Server listening on Unix socket: {SocketPath}", socketPath);
}

try
{
    app.RunAsync(shutdownTokenSource.Token).Wait();
}
catch (OperationCanceledException)
{
    app.Logger.LogInformation("Application shutdown requested.");
}
finally
{
    CleanupResources(app, socketPath);
}

// Graceful shutdown setup method
static void SetupGracefulShutdown(WebApplication app, CancellationTokenSource shutdownTokenSource, string socketPath)
{
    // Handle Ctrl+C (SIGINT) and SIGTERM
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        app.Logger.LogInformation("Ctrl+C received, initiating graceful shutdown...");
        shutdownTokenSource.Cancel();
    };

    // Handle SIGTERM (for Linux/Docker)
    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
    {
        app.Logger.LogInformation("Process exit event received, initiating graceful shutdown...");
        shutdownTokenSource.Cancel();
    };

    // Handle Windows console close events (including Task Manager termination)
    if (OperatingSystem.IsWindows())
    {
        SetConsoleCtrlHandler(eventType =>
        {
            if (eventType == CtrlType.CTRL_C_EVENT ||
                eventType == CtrlType.CTRL_BREAK_EVENT ||
                eventType == CtrlType.CTRL_CLOSE_EVENT ||
                eventType == CtrlType.CTRL_LOGOFF_EVENT ||
                eventType == CtrlType.CTRL_SHUTDOWN_EVENT)
            {
                app.Logger.LogInformation("Windows console event received ({EventType}), initiating graceful shutdown...", eventType);
                shutdownTokenSource.Cancel();
                
                // Give the application time to shut down gracefully
                Thread.Sleep(5000);
                return true;
            }
            return false;
        }, true);
    }
}

// Cleanup resources method
static void CleanupResources(WebApplication app, string socketPath)
{
    app.Logger.LogInformation("Cleaning up resources...");
    
    // Clean up Unix socket file if it exists
    if (!string.IsNullOrEmpty(socketPath) && File.Exists(socketPath))
    {
        try
        {
            File.Delete(socketPath);
            app.Logger.LogInformation("Unix socket file deleted: {SocketPath}", socketPath);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to delete Unix socket file: {SocketPath}", socketPath);
        }
    }
    
    app.Logger.LogInformation("Graceful shutdown completed.");
}

// Windows console event handler
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

delegate bool ConsoleCtrlDelegate(CtrlType ctrlType);

enum CtrlType
{
    CTRL_C_EVENT = 0,
    CTRL_BREAK_EVENT = 1,
    CTRL_CLOSE_EVENT = 2,
    CTRL_LOGOFF_EVENT = 5,
    CTRL_SHUTDOWN_EVENT = 6
}
