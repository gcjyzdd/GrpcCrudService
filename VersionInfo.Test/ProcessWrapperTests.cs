using NUnit.Framework;
using VersionInfo;
using System.Runtime.InteropServices;

namespace VersionInfo.Test;

public class ProcessWrapperTests
{
    private ProcessWrapper _processWrapper;
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [SetUp]
    public void Setup()
    {
        _processWrapper = new ProcessWrapper();
    }

    [Test]
    public void ExecuteProcess_EchoCommand_ReturnsCorrectOutput()
    {
        // Arrange
        string command, arguments;
        if (IsWindows)
        {
            command = "cmd";
            arguments = "/c echo Hello World";
        }
        else
        {
            command = "echo";
            arguments = "Hello World";
        }

        // Act
        var result = _processWrapper.ExecuteProcess(command, arguments);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Is.EqualTo("Hello World"));
        Assert.That(result.StandardError, Is.Empty);
    }

    [Test]
    public void ExecuteProcess_InvalidCommand_ThrowsException()
    {
        // Act & Assert - Both platforms can throw Win32Exception for invalid commands
        Assert.Throws<System.ComponentModel.Win32Exception>(() => 
            _processWrapper.ExecuteProcess("nonexistentcommand12345", ""));
    }

    [Test]
    public void ExecuteProcess_CommandWithError_CapturesStandardError()
    {
        // Arrange & Act - Use a command that writes to stderr
        ProcessExecutionResult result;
        if (IsWindows)
        {
            result = _processWrapper.ExecuteProcess("cmd", "/c echo Error message 1>&2 & exit 2");
        }
        else
        {
            result = _processWrapper.ExecuteProcess("sh", "-c \"echo 'Error message' >&2; exit 2\"");
        }

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
        // Arrange & Act
        ProcessExecutionResult result;
        if (IsWindows)
        {
            result = _processWrapper.ExecuteProcess("cmd", "/c echo test");
        }
        else
        {
            result = _processWrapper.ExecuteProcess("echo", "-n test");
        }

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StandardOutput, Does.Contain("test"));
    }

    [Test]
    public void ExecuteProcess_MultipleCommands_WorksIndependently()
    {
        // Arrange & Act
        ProcessExecutionResult result1, result2;
        if (IsWindows)
        {
            result1 = _processWrapper.ExecuteProcess("cmd", "/c echo first");
            result2 = _processWrapper.ExecuteProcess("cmd", "/c echo second");
        }
        else
        {
            result1 = _processWrapper.ExecuteProcess("echo", "first");
            result2 = _processWrapper.ExecuteProcess("echo", "second");
        }

        // Assert
        Assert.That(result1.ExitCode, Is.EqualTo(0));
        Assert.That(result2.ExitCode, Is.EqualTo(0));
        Assert.That(result1.StandardOutput, Is.EqualTo("first"));
        Assert.That(result2.StandardOutput, Is.EqualTo("second"));
    }
}