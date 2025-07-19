namespace VersionInfo;

public class VersionInfoConfiguration
{
    public string BuildNumberEnvironmentVariable { get; set; } = "BUILD_NUMBER";
    public string LogsDirectory { get; set; } = "logs";
    public string LogFileNamePattern { get; set; } = "versioninfo-.log";
    
    public static VersionInfoConfiguration ForJenkins() => new()
    {
        BuildNumberEnvironmentVariable = "BUILD_NUMBER"
    };
    
    public static VersionInfoConfiguration ForGitLab() => new()
    {
        BuildNumberEnvironmentVariable = "CI_PIPELINE_ID"
    };
    
    public static VersionInfoConfiguration ForGitHubActions() => new()
    {
        BuildNumberEnvironmentVariable = "GITHUB_RUN_NUMBER"
    };
    
    public static VersionInfoConfiguration ForAzureDevOps() => new()
    {
        BuildNumberEnvironmentVariable = "BUILD_BUILDNUMBER"
    };
}