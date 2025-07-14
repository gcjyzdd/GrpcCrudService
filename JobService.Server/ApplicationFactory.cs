using JobService.Infrastructure;
using JobService.Common;
using Serilog;

namespace JobService.Server;

public static class ApplicationFactory
{
    public static Application CreateApplication(string[] args, IConnectionConfiguration? connectionConfig = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        ConfigureSerilog(builder);
        ConfigureServices(builder);
        
        return new Application(builder, connectionConfig);
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/server-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddJobServiceInfrastructure(builder.Configuration);
    }
}