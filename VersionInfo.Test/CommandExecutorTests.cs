using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class CommandExecutorTests
{
    private CommandExecutor _commandExecutor;
    private Mock<IProcessWrapper> _mockProcessWrapper;
    private Mock<ILogger<CommandExecutor>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockProcessWrapper = new Mock<IProcessWrapper>();
        _mockLogger = new Mock<ILogger<CommandExecutor>>();
        _commandExecutor = new CommandExecutor(_mockLogger.Object, _mockProcessWrapper.Object);
    }

    [Test]
    public void ExecuteCommand_SuccessfulExecution_ReturnsSuccessResult()
    {
        // Arrange
        var expectedResult = new ProcessExecutionResult(0, "success output", "");
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Returns(expectedResult);

        // Act
        var result = _commandExecutor.ExecuteCommand("test", "args", "Test error");

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Is.EqualTo("success output"));
        
        // Verify the process wrapper was called with correct parameters
        _mockProcessWrapper.Verify(x => x.ExecuteProcess("test", "args"), Times.Once);
    }

    [Test]
    public void ExecuteCommand_FailedExecution_ReturnsFailureResult()
    {
        // Arrange
        var expectedResult = new ProcessExecutionResult(1, "", "error output");
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Returns(expectedResult);

        // Act
        var result = _commandExecutor.ExecuteCommand("test", "args", "Test error");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Output, Is.EqualTo("error output"));
        
        // Verify the process wrapper was called
        _mockProcessWrapper.Verify(x => x.ExecuteProcess("test", "args"), Times.Once);
    }

    [Test]
    public void ExecuteCommand_ProcessException_ReturnsFailureResult()
    {
        // Arrange
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Throws(new InvalidOperationException("Process failed"));

        // Act
        var result = _commandExecutor.ExecuteCommand("test", "args", "Test error");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Output, Is.Empty);
        
        // Verify the process wrapper was called
        _mockProcessWrapper.Verify(x => x.ExecuteProcess("test", "args"), Times.Once);
    }

    [Test]
    public void ExecuteCommand_SuccessfulExecution_LogsTraceMessages()
    {
        // Arrange
        var expectedResult = new ProcessExecutionResult(0, "success output", "");
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Returns(expectedResult);

        // Act
        _commandExecutor.ExecuteCommand("test", "args", "Test error");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing command")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void ExecuteCommand_FailedExecutionWithError_LogsDebugMessage()
    {
        // Arrange
        var expectedResult = new ProcessExecutionResult(1, "", "command failed");
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Returns(expectedResult);

        // Act
        _commandExecutor.ExecuteCommand("test", "args", "Git command error");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Git command error: command failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void ExecuteCommand_ProcessException_LogsErrorMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Process failed");
        _mockProcessWrapper.Setup(x => x.ExecuteProcess("test", "args"))
            .Throws(exception);

        // Act
        _commandExecutor.ExecuteCommand("test", "args", "Test error");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to execute command")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}