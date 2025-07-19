using NUnit.Framework;
using VersionInfo;

namespace VersionInfo.Test;

public class EnvironmentServiceTests
{
    private EnvironmentService _environmentService;

    [SetUp]
    public void Setup()
    {
        _environmentService = new EnvironmentService();
    }

    [Test]
    public void GetMachineName_ReturnsNonEmptyString()
    {
        // Act
        var result = _environmentService.GetMachineName();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void GetEnvironmentVariable_ExistingVariable_ReturnsValue()
    {
        // Arrange
        var testVarName = "TEMP_TEST_VAR_FOR_UNIT_TEST";
        var testValue = "test_value_123";
        Environment.SetEnvironmentVariable(testVarName, testValue);

        try
        {
            // Act
            var result = _environmentService.GetEnvironmentVariable(testVarName);

            // Assert
            Assert.That(result, Is.EqualTo(testValue));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(testVarName, null);
        }
    }

    [Test]
    public void GetEnvironmentVariable_NonExistingVariable_ReturnsNull()
    {
        // Act
        var result = _environmentService.GetEnvironmentVariable("NON_EXISTING_VAR_123456789");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnvironmentVariable_EmptyVariableName_ReturnsNull()
    {
        // Act
        var result = _environmentService.GetEnvironmentVariable("");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnvironmentVariable_NullVariableName_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _environmentService.GetEnvironmentVariable(null!));
    }
}