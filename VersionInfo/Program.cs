using VersionInfo;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VersionInfo <version>");
    Console.WriteLine("Example: VersionInfo 1.2.3");
    return 1;
}

var environmentService = new EnvironmentService();
var gitService = new GitService();
var versionFormatter = new VersionFormatter();

string inputVersion = args[0];
string hostname = environmentService.GetMachineName();
string? buildNumber = environmentService.GetEnvironmentVariable("BUILD_NUMBER");

string gitTag = gitService.GetGitTag();
string commitHash = gitService.GetShortCommitHash();

string versionOutput = versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);
Console.WriteLine(versionOutput);

return 0;
