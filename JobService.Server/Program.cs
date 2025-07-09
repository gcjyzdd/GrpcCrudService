using JobService.Services;
using JobService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure infrastructure services
builder.Services.AddJobServiceInfrastructure(builder.Configuration);

// Configure Kestrel to use named pipes
var socketPath = string.Empty;
builder.WebHost.ConfigureKestrel(options =>
{
    socketPath = ServerConfiguration.ConfigureKestrel(options);
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
ServerConfiguration.LogConnectionInfo(app.Logger, socketPath);

// Run the application
try
{
    await app.RunAsync(shutdownService.ShutdownToken);
}
catch (OperationCanceledException)
{
    app.Logger.LogInformation("Application shutdown requested.");
}
finally
{
    shutdownService.CleanupResources();
}
