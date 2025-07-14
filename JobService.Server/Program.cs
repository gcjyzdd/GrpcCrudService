using JobService.Server;
using Serilog;

try
{
    var app = ApplicationFactory.CreateApplication(args);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
