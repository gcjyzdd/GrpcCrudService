using JobService.Services;
using JobService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

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
        var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
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
    var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
    app.Logger.LogInformation("gRPC Server listening on Unix socket: {SocketPath}", socketPath);
}

app.Run();
