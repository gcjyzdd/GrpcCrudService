namespace VersionInfo;

public interface IGitService
{
    string GetGitTag();
    string GetShortCommitHash();
}