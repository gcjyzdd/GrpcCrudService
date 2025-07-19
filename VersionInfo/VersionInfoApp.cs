using Microsoft.Extensions.Logging;

namespace VersionInfo;

public class VersionInfoApp(
    ILogger<VersionInfoApp> logger,
    IEnvironmentService environmentService,
    IGitService gitService,
    IVersionFormatter versionFormatter,
    VersionInfoConfiguration configuration)
{
    private readonly ILogger<VersionInfoApp> _logger = logger;
    private readonly IEnvironmentService _environmentService = environmentService;
    private readonly IGitService _gitService = gitService;
    private readonly IVersionFormatter _versionFormatter = versionFormatter;
    private readonly VersionInfoConfiguration _configuration = configuration;

    public Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: VersionInfo <version>");
            Console.WriteLine("Example: VersionInfo 1.2.3");
            return Task.FromResult(1);
        }

        string inputVersion = args[0];

        _logger.LogTrace("VersionInfo starting with input version: {InputVersion}", inputVersion);

        string hostname = _environmentService.GetMachineName();
        string? buildNumber = _environmentService.GetEnvironmentVariable(_configuration.BuildNumberEnvironmentVariable);

        _logger.LogTrace("Environment - Hostname: {Hostname}, BuildNumber: {BuildNumber} (from {EnvVar})",
            hostname, buildNumber ?? "null", _configuration.BuildNumberEnvironmentVariable);

        string gitTag = _gitService.GetGitTag();
        string commitHash = _gitService.GetShortCommitHash();

        _logger.LogTrace("Git info - Tag: {GitTag}, CommitHash: {CommitHash}", gitTag, commitHash);

        string versionOutput = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        _logger.LogTrace("Generated version output: {VersionOutput}", versionOutput);
        _logger.LogInformation(versionOutput);

        return Task.FromResult(0);
    }
}