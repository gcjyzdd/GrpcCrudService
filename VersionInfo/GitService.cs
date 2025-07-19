namespace VersionInfo;

public class GitService : IGitService
{
    private readonly ICommandExecutor _commandExecutor;

    public GitService(ICommandExecutor commandExecutor)
    {
        _commandExecutor = commandExecutor;
    }

    public string GetGitTag()
    {
        var result = _commandExecutor.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error");
        return result.Success ? result.Output : string.Empty;
    }

    public string GetShortCommitHash()
    {
        var result = _commandExecutor.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error");
        return result.Success ? result.Output : "unknown";
    }
}