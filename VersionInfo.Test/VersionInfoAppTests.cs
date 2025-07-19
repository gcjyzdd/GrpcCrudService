using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class VersionInfoAppTests
{
    private VersionInfoApp _versionInfoApp;
    private Mock<ILogger<VersionInfoApp>> _mockLogger;
    private Mock<IEnvironmentService> _mockEnvironmentService;
    private Mock<IGitService> _mockGitService;
    private Mock<IVersionFormatter> _mockVersionFormatter;
    private VersionInfoConfiguration _configuration;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<VersionInfoApp>>();
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockGitService = new Mock<IGitService>();
        _mockVersionFormatter = new Mock<IVersionFormatter>();
        _configuration = VersionInfoConfiguration.ForJenkins();

        _versionInfoApp = new VersionInfoApp(
            _mockLogger.Object,
            _mockEnvironmentService.Object,
            _mockGitService.Object,
            _mockVersionFormatter.Object,
            _configuration);
    }

    [Test]
    public async Task RunAsync_NoArguments_ReturnsErrorCode()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await _versionInfoApp.RunAsync(args);

        // Assert
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task RunAsync_ValidArguments_ReturnsSuccessCode()
    {
        // Arrange
        var args = new[] { "1.2.3" };
        _mockEnvironmentService.Setup(x => x.GetMachineName()).Returns("TestMachine");
        _mockEnvironmentService.Setup(x => x.GetEnvironmentVariable("BUILD_NUMBER")).Returns("123");
        _mockGitService.Setup(x => x.GetGitTag()).Returns("v1.2.3");
        _mockGitService.Setup(x => x.GetShortCommitHash()).Returns("abc1234");
        _mockVersionFormatter.Setup(x => x.FormatVersion("1.2.3", "TestMachine", "v1.2.3", "abc1234", "123"))
            .Returns("1.2.3");

        // Act
        var result = await _versionInfoApp.RunAsync(args);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task RunAsync_CallsAllServicesWithCorrectParameters()
    {
        // Arrange
        var args = new[] { "2.0.0" };
        _mockEnvironmentService.Setup(x => x.GetMachineName()).Returns("DevMachine");
        _mockEnvironmentService.Setup(x => x.GetEnvironmentVariable("BUILD_NUMBER")).Returns((string?)null);
        _mockGitService.Setup(x => x.GetGitTag()).Returns("");
        _mockGitService.Setup(x => x.GetShortCommitHash()).Returns("def5678");
        _mockVersionFormatter.Setup(x => x.FormatVersion("2.0.0", "DevMachine", "", "def5678", null))
            .Returns("2.0.0-developerbuild");

        // Act
        await _versionInfoApp.RunAsync(args);

        // Assert
        _mockEnvironmentService.Verify(x => x.GetMachineName(), Times.Once);
        _mockEnvironmentService.Verify(x => x.GetEnvironmentVariable("BUILD_NUMBER"), Times.Once);
        _mockGitService.Verify(x => x.GetGitTag(), Times.Once);
        _mockGitService.Verify(x => x.GetShortCommitHash(), Times.Once);
        _mockVersionFormatter.Verify(x => x.FormatVersion("2.0.0", "DevMachine", "", "def5678", null), Times.Once);
    }

    [Test]
    public async Task RunAsync_GitLabConfiguration_UsesCorrectEnvironmentVariable()
    {
        // Arrange
        var gitLabConfig = VersionInfoConfiguration.ForGitLab();
        var gitLabApp = new VersionInfoApp(
            _mockLogger.Object,
            _mockEnvironmentService.Object,
            _mockGitService.Object,
            _mockVersionFormatter.Object,
            gitLabConfig);

        var args = new[] { "1.0.0" };
        _mockEnvironmentService.Setup(x => x.GetMachineName()).Returns("GitLabRunner");
        _mockEnvironmentService.Setup(x => x.GetEnvironmentVariable("CI_PIPELINE_ID")).Returns("456");
        _mockGitService.Setup(x => x.GetGitTag()).Returns("");
        _mockGitService.Setup(x => x.GetShortCommitHash()).Returns("xyz789");
        _mockVersionFormatter.Setup(x => x.FormatVersion("1.0.0", "GitLabRunner", "", "xyz789", "456"))
            .Returns("1.0.0-gitlab");

        // Act
        await gitLabApp.RunAsync(args);

        // Assert
        _mockEnvironmentService.Verify(x => x.GetEnvironmentVariable("CI_PIPELINE_ID"), Times.Once);
        _mockEnvironmentService.Verify(x => x.GetEnvironmentVariable("BUILD_NUMBER"), Times.Never);
    }

    [Test]
    public async Task RunAsync_LogsTraceInformation()
    {
        // Arrange
        var args = new[] { "3.1.0" };
        _mockEnvironmentService.Setup(x => x.GetMachineName()).Returns("LogTestMachine");
        _mockEnvironmentService.Setup(x => x.GetEnvironmentVariable("BUILD_NUMBER")).Returns("789");
        _mockGitService.Setup(x => x.GetGitTag()).Returns("v3.1.0_RC1");
        _mockGitService.Setup(x => x.GetShortCommitHash()).Returns("ghi012");
        _mockVersionFormatter.Setup(x => x.FormatVersion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("3.1.0a-RC1");

        // Act
        await _versionInfoApp.RunAsync(args);

        // Assert - Verify trace logs are written
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("VersionInfo starting with input version: 3.1.0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Environment - Hostname: LogTestMachine, BuildNumber: 789")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Git info - Tag: v3.1.0_RC1, CommitHash: ghi012")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("3.1.0a-RC1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RunAsync_EmptyStringArgument_ProcessesEmptyVersion()
    {
        // Arrange
        var args = new[] { "" };
        _mockEnvironmentService.Setup(x => x.GetMachineName()).Returns("TestMachine");
        _mockEnvironmentService.Setup(x => x.GetEnvironmentVariable("BUILD_NUMBER")).Returns((string?)null);
        _mockGitService.Setup(x => x.GetGitTag()).Returns("");
        _mockGitService.Setup(x => x.GetShortCommitHash()).Returns("abc123");
        _mockVersionFormatter.Setup(x => x.FormatVersion("", "TestMachine", "", "abc123", null))
            .Returns("-developerbuild");

        // Act
        var result = await _versionInfoApp.RunAsync(args);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _mockVersionFormatter.Verify(x => x.FormatVersion("", "TestMachine", "", "abc123", null), Times.Once);
    }
}