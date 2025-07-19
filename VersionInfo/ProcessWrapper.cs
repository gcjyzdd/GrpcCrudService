using System.Diagnostics;

namespace VersionInfo;

public class ProcessWrapper : IProcessWrapper
{
    public ProcessExecutionResult ExecuteProcess(string fileName, string arguments)
    {
        using var process = new Process
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

        return new ProcessExecutionResult(process.ExitCode, output, error);
    }
}