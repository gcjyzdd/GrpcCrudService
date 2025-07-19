using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VersionInfo;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VersionInfo <version>");
    Console.WriteLine("Example: VersionInfo 1.2.3");
    return 1;
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        outputTemplate: "{Message:lj}{NewLine}")
    .WriteTo.File("logs/versioninfo-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
    .CreateLogger();

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog();
});

services.AddTransient<IEnvironmentService, EnvironmentService>();
services.AddTransient<ICommandExecutor, CommandExecutor>();
services.AddTransient<IGitService, GitService>();
services.AddTransient<IVersionFormatter, VersionFormatter>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogTrace("VersionInfo starting with input version: {InputVersion}", args[0]);

var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
var gitService = serviceProvider.GetRequiredService<IGitService>();
var versionFormatter = serviceProvider.GetRequiredService<IVersionFormatter>();

string inputVersion = args[0];
string hostname = environmentService.GetMachineName();
string? buildNumber = environmentService.GetEnvironmentVariable("BUILD_NUMBER");

logger.LogTrace("Environment - Hostname: {Hostname}, BuildNumber: {BuildNumber}", hostname, buildNumber ?? "null");

string gitTag = gitService.GetGitTag();
string commitHash = gitService.GetShortCommitHash();

logger.LogTrace("Git info - Tag: {GitTag}, CommitHash: {CommitHash}", gitTag, commitHash);

string versionOutput = versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

logger.LogTrace("Generated version output: {VersionOutput}", versionOutput);

logger.LogInformation(versionOutput);
return 0;
