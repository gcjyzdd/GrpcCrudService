using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace VersionInfo;

public static class VersionInfoAppFactory
{
    public static VersionInfoApp CreateApp(VersionInfoConfiguration? configuration = null)
    {
        configuration ??= VersionInfoConfiguration.ForJenkins(); // Default to Jenkins
        
        ConfigureLogging(configuration);
        
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        
        return serviceProvider.GetRequiredService<VersionInfoApp>();
    }
    
    public static VersionInfoApp CreateForJenkins()
    {
        return CreateApp(VersionInfoConfiguration.ForJenkins());
    }
    
    public static VersionInfoApp CreateForGitLab()
    {
        return CreateApp(VersionInfoConfiguration.ForGitLab());
    }
    
    public static VersionInfoApp CreateForGitHubActions()
    {
        return CreateApp(VersionInfoConfiguration.ForGitHubActions());
    }
    
    public static VersionInfoApp CreateForAzureDevOps()
    {
        return CreateApp(VersionInfoConfiguration.ForAzureDevOps());
    }
    
    private static void ConfigureLogging(VersionInfoConfiguration configuration)
    {
        var logFilePath = Path.Combine(configuration.LogsDirectory, configuration.LogFileNamePattern);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                outputTemplate: "{Message:lj}{NewLine}")
            .WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
    }
    
    private static void ConfigureServices(IServiceCollection services, VersionInfoConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        services.AddSingleton(configuration);
        services.AddTransient<IEnvironmentService, EnvironmentService>();
        services.AddTransient<ICommandExecutor, CommandExecutor>();
        services.AddTransient<IGitService, GitService>();
        services.AddTransient<IVersionFormatter, VersionFormatter>();
        services.AddTransient<VersionInfoApp>();
    }
}