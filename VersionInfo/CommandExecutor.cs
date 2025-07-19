using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VersionInfo;

public class CommandExecutor : ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;

    public CommandExecutor(ILogger<CommandExecutor> logger)
    {
        _logger = logger;
    }

    public CommandResult ExecuteCommand(string fileName, string arguments, string? errorPrefix = null)
    {
        _logger.LogTrace("Executing command: {FileName} {Arguments}", fileName, arguments);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            _logger.LogTrace("Command completed with exit code {ExitCode}. Output: {Output}", process.ExitCode, output);

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(errorPrefix))
            {
                _logger.LogDebug("{ErrorPrefix}: {Error}", errorPrefix, error);
            }

            return new CommandResult(process.ExitCode == 0, process.ExitCode == 0 ? output : error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {FileName} {Arguments}", fileName, arguments);
            return new CommandResult(false, string.Empty);
        }
    }
}