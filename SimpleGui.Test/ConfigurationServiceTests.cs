using System.Text.Json;
using Moq;
using NUnit.Framework;
using SimpleGui.Models;
using SimpleGui.Services;
using Wrappers;

namespace SimpleGui.Test;

[TestFixture]
public class ConfigurationServiceTests
{
    private Mock<IFileWrapper> _mockFileWrapper = null!;
    private ConfigurationService _configurationService = null!;
    private AppConfiguration _testConfig = null!;
    private string _testConfigJson = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFileWrapper = new Mock<IFileWrapper>();
        _configurationService = new ConfigurationService(_mockFileWrapper.Object);

        _testConfig = new AppConfiguration
        {
            UserName = "Test User",
            Theme = "Dark",
            AutoSave = true,
            ServerUrl = "https://test.example.com",
            LastModified = new DateTime(2023, 12, 25, 10, 30, 0)
        };

        _testConfigJson = JsonSerializer.Serialize(_testConfig, AppConfigurationContext.Default.AppConfiguration);
    }

    [Test]
    public void Constructor_WithValidFileWrapper_InitializesCorrectly()
    {
        // Act & Assert
        Assert.That(_configurationService, Is.Not.Null);
        Assert.That(_configurationService, Is.InstanceOf<IConfigurationService>());
    }

    [Test]
    public void GetDefaultConfiguration_ReturnsCorrectDefaults()
    {
        // Act
        var defaultConfig = _configurationService.GetDefaultConfiguration();

        // Assert
        Assert.That(defaultConfig, Is.Not.Null);
        Assert.That(defaultConfig.UserName, Is.EqualTo("Default User"));
        Assert.That(defaultConfig.Theme, Is.EqualTo("Dark"));
        Assert.That(defaultConfig.AutoSave, Is.True);
        Assert.That(defaultConfig.ServerUrl, Is.EqualTo("localhost:5000"));
        Assert.That(defaultConfig.LastModified, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task LoadConfigurationAsync_WithExistingFile_ReturnsConfigurationFromFile()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _mockFileWrapper.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync(_testConfigJson);

        // Act
        var result = await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserName, Is.EqualTo(_testConfig.UserName));
        Assert.That(result.Theme, Is.EqualTo(_testConfig.Theme));
        Assert.That(result.AutoSave, Is.EqualTo(_testConfig.AutoSave));
        Assert.That(result.ServerUrl, Is.EqualTo(_testConfig.ServerUrl));
        Assert.That(result.LastModified, Is.EqualTo(_testConfig.LastModified));

        _mockFileWrapper.Verify(x => x.Exists(It.IsAny<string>()), Times.Once);
        _mockFileWrapper.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task LoadConfigurationAsync_WithNonExistingFile_CreatesAndReturnsDefaultConfiguration()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserName, Is.EqualTo("Default User"));
        Assert.That(result.Theme, Is.EqualTo("Dark"));
        Assert.That(result.AutoSave, Is.True);
        Assert.That(result.ServerUrl, Is.EqualTo("localhost:5000"));

        _mockFileWrapper.Verify(x => x.Exists(It.IsAny<string>()), Times.Once);
        _mockFileWrapper.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task LoadConfigurationAsync_WithCorruptedJson_ReturnsDefaultConfiguration()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _mockFileWrapper.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync("{ invalid json }");

        // Act
        var result = await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserName, Is.EqualTo("Default User"));
        Assert.That(result.Theme, Is.EqualTo("Dark"));
        Assert.That(result.AutoSave, Is.True);
        Assert.That(result.ServerUrl, Is.EqualTo("localhost:5000"));

        _mockFileWrapper.Verify(x => x.Exists(It.IsAny<string>()), Times.Once);
        _mockFileWrapper.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task LoadConfigurationAsync_WithNullJsonDeserialization_ReturnsDefaultConfiguration()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _mockFileWrapper.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync("null");

        // Act
        var result = await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserName, Is.EqualTo("Default User"));

        _mockFileWrapper.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task LoadConfigurationAsync_WithFileReadException_ReturnsDefaultConfiguration()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _mockFileWrapper.Setup(x => x.ReadAllTextAsync(It.IsAny<string>())).ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserName, Is.EqualTo("Default User"));

        _mockFileWrapper.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SaveConfigurationAsync_WithValidConfiguration_SavesCorrectly()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _configurationService.SaveConfigurationAsync(_testConfig);

        // Assert
        _mockFileWrapper.Verify(x => x.WriteAllTextAsync(
            It.IsAny<string>(),
            It.Is<string>(json => json.Contains(_testConfig.UserName) &&
                                 json.Contains(_testConfig.Theme) &&
                                 json.Contains(_testConfig.ServerUrl))
        ), Times.Once);
    }

    [Test]
    public async Task SaveConfigurationAsync_UpdatesLastModifiedTimestamp()
    {
        // Arrange
        var originalLastModified = _testConfig.LastModified;
        var capturedJson = string.Empty;

        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Callback<string, string>((path, content) => capturedJson = content)
                        .Returns(Task.CompletedTask);

        // Act
        await _configurationService.SaveConfigurationAsync(_testConfig);

        // Assert
        var savedConfig = JsonSerializer.Deserialize(capturedJson, AppConfigurationContext.Default.AppConfiguration);
        Assert.That(savedConfig, Is.Not.Null);
        Assert.That(savedConfig.LastModified, Is.GreaterThan(originalLastModified));
        Assert.That(savedConfig.LastModified, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void SaveConfigurationAsync_WithWriteException_HandlesGracefully()
    {
        // Arrange
        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act & Assert - Should not throw exception
        Assert.DoesNotThrowAsync(async () => await _configurationService.SaveConfigurationAsync(_testConfig));

        _mockFileWrapper.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void SaveConfigurationAsync_WithJsonSerializationError_HandlesGracefully()
    {
        // This test is harder to trigger since we're using source generators,
        // but we test with a configuration that could potentially cause issues

        // Arrange
        var problematicConfig = new AppConfiguration
        {
            UserName = new string('x', 10000), // Very long string
            Theme = "Test",
            AutoSave = true,
            ServerUrl = "https://test.com"
        };

        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act & Assert - Should not throw exception
        Assert.DoesNotThrowAsync(async () => await _configurationService.SaveConfigurationAsync(problematicConfig));
    }

    [Test]
    public async Task LoadConfigurationAsync_FilePathIsCorrect()
    {
        // Arrange
        var expectedPathParts = new[] { "SimpleGui", "config.json" };
        string? capturedPath = null;

        _mockFileWrapper.Setup(x => x.Exists(It.IsAny<string>()))
                        .Callback<string>(path => capturedPath = path)
                        .Returns(false);
        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _configurationService.LoadConfigurationAsync();

        // Assert
        Assert.That(capturedPath, Is.Not.Null);
        foreach (var pathPart in expectedPathParts)
        {
            Assert.That(capturedPath, Contains.Substring(pathPart));
        }
    }

    [Test]
    public async Task SaveConfigurationAsync_FilePathIsCorrect()
    {
        // Arrange
        var expectedPathParts = new[] { "SimpleGui", "config.json" };
        string? capturedPath = null;

        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Callback<string, string>((path, content) => capturedPath = path)
                        .Returns(Task.CompletedTask);

        // Act
        await _configurationService.SaveConfigurationAsync(_testConfig);

        // Assert
        Assert.That(capturedPath, Is.Not.Null);
        foreach (var pathPart in expectedPathParts)
        {
            Assert.That(capturedPath, Contains.Substring(pathPart));
        }
    }

    [Test]
    public async Task SaveConfigurationAsync_PreservesAllConfigurationProperties()
    {
        // Arrange
        string capturedJson = string.Empty;
        _mockFileWrapper.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .Callback<string, string>((path, content) => capturedJson = content)
                        .Returns(Task.CompletedTask);

        // Act
        await _configurationService.SaveConfigurationAsync(_testConfig);

        // Assert
        var savedConfig = JsonSerializer.Deserialize(capturedJson, AppConfigurationContext.Default.AppConfiguration);
        Assert.That(savedConfig, Is.Not.Null);
        Assert.That(savedConfig.UserName, Is.EqualTo(_testConfig.UserName));
        Assert.That(savedConfig.Theme, Is.EqualTo(_testConfig.Theme));
        Assert.That(savedConfig.AutoSave, Is.EqualTo(_testConfig.AutoSave));
        Assert.That(savedConfig.ServerUrl, Is.EqualTo(_testConfig.ServerUrl));
    }
}