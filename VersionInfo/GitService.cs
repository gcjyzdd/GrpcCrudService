using System.Diagnostics;

namespace VersionInfo;

public class GitService : IGitService
{
    public string GetGitTag()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "describe --exact-match --tags HEAD",
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
            
            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Git tag error: {error}");
            }
            
            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public string GetShortCommitHash()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
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
            
            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Git commit hash error: {error}");
            }
            
            return process.ExitCode == 0 ? output : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}