namespace VersionInfo;

public interface ICommandExecutor
{
    CommandResult ExecuteCommand(string fileName, string arguments, string? errorPrefix = null);
}

public readonly record struct CommandResult(bool Success, string Output);