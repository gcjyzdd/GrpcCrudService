using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VersionInfo;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VersionInfo <version>");
    Console.WriteLine("Example: VersionInfo 1.2.3");
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

services.AddTransient<IEnvironmentService, EnvironmentService>();
services.AddTransient<ICommandExecutor, CommandExecutor>();
services.AddTransient<IGitService, GitService>();
services.AddTransient<IVersionFormatter, VersionFormatter>();

var serviceProvider = services.BuildServiceProvider();

var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
var gitService = serviceProvider.GetRequiredService<IGitService>();
var versionFormatter = serviceProvider.GetRequiredService<IVersionFormatter>();

string inputVersion = args[0];
string hostname = environmentService.GetMachineName();
string? buildNumber = environmentService.GetEnvironmentVariable("BUILD_NUMBER");

string gitTag = gitService.GetGitTag();
string commitHash = gitService.GetShortCommitHash();

string versionOutput = versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);
Console.WriteLine(versionOutput);

return 0;
