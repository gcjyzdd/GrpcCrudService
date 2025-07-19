namespace VersionInfo;

public class VersionFormatter : IVersionFormatter
{
    public string FormatVersion(string inputVersion, string hostname, string gitTag, string commitHash, string? buildNumber)
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
}