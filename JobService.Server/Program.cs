using JobService.Services;
using JobService.Infrastructure;
using JobService.Common;

var builder = WebApplication.CreateBuilder(args);

// Configure infrastructure services
builder.Services.AddJobServiceInfrastructure(builder.Configuration);

// Register connection configuration
var connectionConfig = new ConnectionConfiguration();

// Override with test configuration if in testing environment
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

// Configure Kestrel to use named pipes
var socketPath = string.Empty;
builder.WebHost.ConfigureKestrel(options =>
{
    socketPath = ServerConfiguration.ConfigureKestrel(options, connectionConfig);
});

var app = builder.Build();

// Get required services
var shutdownTokenSource = app.Services.GetRequiredService<CancellationTokenSource>();
var shutdownService = new GracefulShutdownService(
    app.Services.GetRequiredService<ILogger<GracefulShutdownService>>(),
    shutdownTokenSource,
    socketPath);

// Setup graceful shutdown handling
shutdownService.SetupShutdownHandlers();

// Initialize database
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

// Configure the gRPC pipeline
app.MapGrpcService<JobGrpcService>();

// Log connection information
ServerConfiguration.LogConnectionInfo(app.Logger, connectionConfig);

// Run the application
try
{
    await app.RunAsync(shutdownService.ShutdownToken);
}
catch (OperationCanceledException ex)
{
    app.Logger.LogInformation(ex, "Application shutdown requested.");
}
finally
{
    shutdownService.CleanupResources();
}
