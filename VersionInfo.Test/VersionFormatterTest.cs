using VersionInfo;

namespace VersionInfo.Test;

public class VersionFormatterTest
{
    private VersionFormatter _versionFormatter;

    [SetUp]
    public void Setup()
    {
        _versionFormatter = new VersionFormatter();
    }

    [Test]
    public void FormatVersion_NonJenkinsAgent_ReturnsDeveloperBuild()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "DeveloperMachine";
        string gitTag = "";
        string commitHash = "abc1234";
        string? buildNumber = null;

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3-developerbuild"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithoutGitTagAndWithoutBuildNumber_ReturnsVersionWithCommitHash()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "JenkinsAgent";
        string gitTag = "";
        string commitHash = "abc1234";
        string? buildNumber = null;

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3a-abc1234"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithoutGitTagButWithBuildNumber_ReturnsVersionWithCommitHashAndBuildNumber()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "JenkinsAgent";
        string gitTag = "";
        string commitHash = "abc1234";
        string buildNumber = "456";

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3a-abc1234-456"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithRCGitTag_ReturnsVersionWithRC()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "JenkinsAgent";
        string gitTag = "1.2.3_RC1";
        string commitHash = "abc1234";
        string? buildNumber = null;

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3a-RC1"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithExactGitTag_ReturnsExactVersion()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "JenkinsAgent";
        string gitTag = "1.2.3";
        string commitHash = "abc1234";
        string? buildNumber = null;

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentCaseInsensitive_ReturnsBuildVersion()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "jenkinsagent"; // lowercase
        string gitTag = "";
        string commitHash = "abc1234";
        string? buildNumber = "123";

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3a-abc1234-123"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithDifferentRCTag_ReturnsVersionWithRC()
    {
        // Arrange
        string inputVersion = "2.5.1";
        string hostname = "JenkinsAgent";
        string gitTag = "2.5.1_RC3";
        string commitHash = "def5678";
        string? buildNumber = "789";

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("2.5.1a-RC3"));
    }

    [Test]
    public void FormatVersion_JenkinsAgentWithNonMatchingGitTag_ReturnsVersionWithCommitHashAndBuildNumber()
    {
        // Arrange
        string inputVersion = "1.2.3";
        string hostname = "JenkinsAgent";
        string gitTag = "1.2.4"; // Different tag
        string commitHash = "abc1234";
        string buildNumber = "456";

        // Act
        string result = _versionFormatter.FormatVersion(inputVersion, hostname, gitTag, commitHash, buildNumber);

        // Assert
        Assert.That(result, Is.EqualTo("1.2.3a-abc1234-456"));
    }
}