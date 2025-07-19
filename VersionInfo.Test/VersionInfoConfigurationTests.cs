using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class VersionInfoConfigurationTests
{
    [Test]
    public void ForJenkins_ReturnsCorrectConfiguration()
    {
        // Act
        var config = VersionInfoConfiguration.ForJenkins();

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("BUILD_NUMBER"));
        Assert.That(config.LogsDirectory, Is.EqualTo("logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("versioninfo-.log"));
    }

    [Test]
    public void ForGitLab_ReturnsCorrectConfiguration()
    {
        // Act
        var config = VersionInfoConfiguration.ForGitLab();

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("CI_PIPELINE_ID"));
        Assert.That(config.LogsDirectory, Is.EqualTo("logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("versioninfo-.log"));
    }

    [Test]
    public void ForGitHubActions_ReturnsCorrectConfiguration()
    {
        // Act
        var config = VersionInfoConfiguration.ForGitHubActions();

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("GITHUB_RUN_NUMBER"));
        Assert.That(config.LogsDirectory, Is.EqualTo("logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("versioninfo-.log"));
    }

    [Test]
    public void ForAzureDevOps_ReturnsCorrectConfiguration()
    {
        // Act
        var config = VersionInfoConfiguration.ForAzureDevOps();

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("BUILD_BUILDNUMBER"));
        Assert.That(config.LogsDirectory, Is.EqualTo("logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("versioninfo-.log"));
    }

    [Test]
    public void DefaultConfiguration_HasCorrectDefaults()
    {
        // Act
        var config = new VersionInfoConfiguration();

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("BUILD_NUMBER"));
        Assert.That(config.LogsDirectory, Is.EqualTo("logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("versioninfo-.log"));
    }

    [Test]
    public void Configuration_CanBeModified()
    {
        // Arrange
        var config = new VersionInfoConfiguration();

        // Act
        config.BuildNumberEnvironmentVariable = "CUSTOM_BUILD_VAR";
        config.LogsDirectory = "custom-logs";
        config.LogFileNamePattern = "custom-.log";

        // Assert
        Assert.That(config.BuildNumberEnvironmentVariable, Is.EqualTo("CUSTOM_BUILD_VAR"));
        Assert.That(config.LogsDirectory, Is.EqualTo("custom-logs"));
        Assert.That(config.LogFileNamePattern, Is.EqualTo("custom-.log"));
    }

    [Test]
    public void AllFactoryMethods_ReturnDifferentInstances()
    {
        // Act
        var jenkins = VersionInfoConfiguration.ForJenkins();
        var gitlab = VersionInfoConfiguration.ForGitLab();
        var github = VersionInfoConfiguration.ForGitHubActions();
        var azure = VersionInfoConfiguration.ForAzureDevOps();

        // Assert
        Assert.That(jenkins, Is.Not.SameAs(gitlab));
        Assert.That(gitlab, Is.Not.SameAs(github));
        Assert.That(github, Is.Not.SameAs(azure));
        Assert.That(azure, Is.Not.SameAs(jenkins));
    }
}