using VersionInfo;

// Detect CI environment and create appropriate app
var app = DetectCiEnvironment();

return await app.RunAsync(args);

static VersionInfoApp DetectCiEnvironment()
{
    // Check for GitLab CI
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITLAB_CI")))
    {
        return VersionInfoAppFactory.CreateForGitLab();
    }
    
    // Check for GitHub Actions
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
    {
        return VersionInfoAppFactory.CreateForGitHubActions();
    }
    
    // Check for Azure DevOps
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_HTTP_USER_AGENT")))
    {
        return VersionInfoAppFactory.CreateForAzureDevOps();
    }
    
    // Default to Jenkins
    return VersionInfoAppFactory.CreateForJenkins();
}
