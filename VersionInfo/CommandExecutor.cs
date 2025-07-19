using Microsoft.Extensions.Logging;

namespace VersionInfo;

public class CommandExecutor : ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;
    private readonly IProcessWrapper _processWrapper;

    public CommandExecutor(ILogger<CommandExecutor> logger, IProcessWrapper processWrapper)
    {
        _logger = logger;
        _processWrapper = processWrapper;
    }

    public CommandResult ExecuteCommand(string fileName, string arguments, string? errorPrefix = null)
    {
        _logger.LogTrace("Executing command: {FileName} {Arguments}", fileName, arguments);

        try
        {
            var result = _processWrapper.ExecuteProcess(fileName, arguments);

            _logger.LogTrace("Command completed with exit code {ExitCode}. Output: {Output}", result.ExitCode, result.StandardOutput);

            if (result.ExitCode != 0 && !string.IsNullOrEmpty(result.StandardError) && !string.IsNullOrEmpty(errorPrefix))
            {
                _logger.LogDebug("{ErrorPrefix}: {Error}", errorPrefix, result.StandardError);
            }

            return new CommandResult(result.ExitCode == 0, result.ExitCode == 0 ? result.StandardOutput : result.StandardError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {FileName} {Arguments}", fileName, arguments);
            return new CommandResult(false, string.Empty);
        }
    }
}