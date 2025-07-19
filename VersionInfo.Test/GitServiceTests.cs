using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class GitServiceTests
{
    private GitService _gitService;
    private Mock<ICommandExecutor> _mockCommandExecutor;

    [SetUp]
    public void Setup()
    {
        _mockCommandExecutor = new Mock<ICommandExecutor>();
        _gitService = new GitService(_mockCommandExecutor.Object);
    }

    [Test]
    public void GetGitTag_SuccessfulExecution_ReturnsTag()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error"))
            .Returns(new CommandResult(true, "v1.2.3"));

        // Act
        var result = _gitService.GetGitTag();

        // Assert
        Assert.That(result, Is.EqualTo("v1.2.3"));
        _mockCommandExecutor.Verify(x => x.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error"), Times.Once);
    }

    [Test]
    public void GetGitTag_FailedExecution_ReturnsEmptyString()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error"))
            .Returns(new CommandResult(false, "fatal: No names found, cannot describe anything."));

        // Act
        var result = _gitService.GetGitTag();

        // Assert
        Assert.That(result, Is.Empty);
        _mockCommandExecutor.Verify(x => x.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error"), Times.Once);
    }

    [Test]
    public void GetShortCommitHash_SuccessfulExecution_ReturnsCommitHash()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error"))
            .Returns(new CommandResult(true, "abc1234"));

        // Act
        var result = _gitService.GetShortCommitHash();

        // Assert
        Assert.That(result, Is.EqualTo("abc1234"));
        _mockCommandExecutor.Verify(x => x.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error"), Times.Once);
    }

    [Test]
    public void GetShortCommitHash_FailedExecution_ReturnsUnknown()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error"))
            .Returns(new CommandResult(false, "fatal: not a git repository"));

        // Act
        var result = _gitService.GetShortCommitHash();

        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
        _mockCommandExecutor.Verify(x => x.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error"), Times.Once);
    }

    [Test]
    public void GetGitTag_EmptyOutput_ReturnsEmptyString()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "describe --exact-match --tags HEAD", "Git tag error"))
            .Returns(new CommandResult(true, ""));

        // Act
        var result = _gitService.GetGitTag();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetShortCommitHash_EmptyOutput_ReturnsEmptyString()
    {
        // Arrange
        _mockCommandExecutor.Setup(x => x.ExecuteCommand("git", "rev-parse --short HEAD", "Git commit hash error"))
            .Returns(new CommandResult(true, ""));

        // Act
        var result = _gitService.GetShortCommitHash();

        // Assert
        Assert.That(result, Is.Empty);
    }
}