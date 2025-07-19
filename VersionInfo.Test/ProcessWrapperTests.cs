using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class ProcessWrapperTests
{
    private ProcessWrapper _processWrapper;

    [SetUp]
    public void Setup()
    {
        _processWrapper = new ProcessWrapper();
    }

    [Test]
    public void ExecuteProcess_EchoCommand_ReturnsCorrectOutput()
    {
        // Act
        var result = _processWrapper.ExecuteProcess("echo", "Hello World");

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Is.EqualTo("Hello World"));
        Assert.That(result.StandardError, Is.Empty);
    }

    [Test]
    public void ExecuteProcess_InvalidCommand_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<System.ComponentModel.Win32Exception>(() => 
            _processWrapper.ExecuteProcess("nonexistentcommand12345", ""));
    }

    [Test]
    public void ExecuteProcess_CommandWithError_CapturesStandardError()
    {
        // Act - Use a command that writes to stderr
        var result = _processWrapper.ExecuteProcess("sh", "-c \"echo 'Error message' >&2; exit 2\"");

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(2));
        Assert.That(result.StandardError, Does.Contain("Error message"));
    }

    [Test]
    public void ExecuteProcess_EmptyCommand_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            _processWrapper.ExecuteProcess("", ""));
    }

    [Test]
    public void ExecuteProcess_ValidCommandWithArguments_ExecutesCorrectly()
    {
        // Act
        var result = _processWrapper.ExecuteProcess("echo", "-n test");

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Does.Contain("test"));
    }

    [Test]
    public void ExecuteProcess_MultipleCommands_WorksIndependently()
    {
        // Act
        var result1 = _processWrapper.ExecuteProcess("echo", "first");
        var result2 = _processWrapper.ExecuteProcess("echo", "second");

        // Assert
        Assert.That(result1.ExitCode, Is.EqualTo(0));
        Assert.That(result2.ExitCode, Is.EqualTo(0));
        Assert.That(result1.StandardOutput, Is.EqualTo("first"));
        Assert.That(result2.StandardOutput, Is.EqualTo("second"));
    }
}