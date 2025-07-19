using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Usage: VersionInfo <version>");
    Console.WriteLine("Example: VersionInfo 1.2.3");
    return 1;
}

string inputVersion = args[0];
string hostname = Environment.MachineName;
string? buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");

string gitTag = GetGitTag();
string commitHash = GetShortCommitHash();

string versionOutput = FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);
Console.WriteLine(versionOutput);

return 0;

static string GetGitTag()
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
        process.WaitForExit();
        
        return process.ExitCode == 0 ? output : string.Empty;
    }
    catch
    {
        return string.Empty;
    }
}

static string GetShortCommitHash()
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
        process.WaitForExit();
        
        return process.ExitCode == 0 ? output : "unknown";
    }
    catch
    {
        return "unknown";
    }
}

static string FormatVersion(string inputVersion, string hostname, string gitTag, string commitHash, string? buildNumber)
{
    bool isJenkinsAgent = hostname.Equals("JenkinsAgent", StringComparison.OrdinalIgnoreCase);
    
    if (!isJenkinsAgent)
    {
        return $"{inputVersion}-developerbuild";
    }
    
    if (!string.IsNullOrEmpty(gitTag))
    {
        if (gitTag.StartsWith($"{inputVersion}_RC"))
        {
            string rcPart = gitTag.Substring($"{inputVersion}_".Length);
            return $"{inputVersion}a-{rcPart}";
        }
        else if (gitTag == inputVersion)
        {
            return inputVersion;
        }
    }
    
    if (!string.IsNullOrEmpty(buildNumber))
    {
        return $"{inputVersion}a-{commitHash}-{buildNumber}";
    }
    
    return $"{inputVersion}a-{commitHash}";
}
