namespace VersionInfo;

public interface IVersionFormatter
{
    string FormatVersion(string inputVersion, string hostname, string gitTag, string commitHash, string? buildNumber);
}