using System.Diagnostics;

namespace VersionInfo;

public class CommandExecutor : ICommandExecutor
{
    public CommandResult ExecuteCommand(string fileName, string arguments, string? errorPrefix = null)
    {
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

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(errorPrefix))
            {
                Console.WriteLine($"{errorPrefix}: {error}");
            }

            return new CommandResult(process.ExitCode == 0, process.ExitCode == 0 ? output : error);
        }
        catch
        {
            return new CommandResult(false, string.Empty);
        }
    }
}