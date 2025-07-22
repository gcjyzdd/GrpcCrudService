using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moq;
using NUnit.Framework;
using SimpleGui.Models;
using SimpleGui.Services;
using SimpleGui.ViewModels;

namespace SimpleGui.Test;

[TestFixture]
public class MainWindowViewModelTests
{
    private Mock<IConfigurationService> _mockConfigurationService = null!;
    private MainWindowViewModel _viewModel = null!;
    private AppConfiguration _testConfig = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfigurationService = new Mock<IConfigurationService>();
        _testConfig = new AppConfiguration
        {
            UserName = "Test User",
            Theme = "Light",
            AutoSave = true,
            ServerUrl = "https://test.server.com",
            LastModified = DateTime.Now
        };
        
        // Setup default behavior for configuration service
        _mockConfigurationService
            .Setup(x => x.LoadConfigurationAsync())
            .ReturnsAsync(_testConfig);
    }

    [Test]
    public void Constructor_WithValidConfigurationService_InitializesCorrectly()
    {
        // Act
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);

        // Assert
        Assert.That(_viewModel, Is.Not.Null);
        Assert.That(_viewModel.Greeting, Is.EqualTo("Welcome to Avalonia!"));
        Assert.That(_viewModel.LoadConfigurationCommand, Is.Not.Null);
        Assert.That(_viewModel.SaveConfigurationCommand, Is.Not.Null);
        Assert.That(_viewModel.Status, Is.Not.Null);
    }

    [Test]
    public async Task Constructor_InitializesPropertiesFromConfiguration()
    {
        // Act
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        
        // Wait for async loading to complete
        await Task.Delay(200);

        // Assert - Properties should be populated from loaded configuration
        Assert.That(_viewModel.UserName, Is.EqualTo(_testConfig.UserName));
        Assert.That(_viewModel.Theme, Is.EqualTo(_testConfig.Theme));
        Assert.That(_viewModel.AutoSave, Is.EqualTo(_testConfig.AutoSave));
        Assert.That(_viewModel.ServerUrl, Is.EqualTo(_testConfig.ServerUrl));
    }

    [Test]
    public async Task LoadConfigurationCommand_LoadsConfigurationSuccessfully()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        
        // Wait for constructor's async load to complete
        await Task.Delay(100);

        // Act - Execute load command explicitly
        if (_viewModel.LoadConfigurationCommand is IAsyncRelayCommand asyncCommand)
            await asyncCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.UserName, Is.EqualTo(_testConfig.UserName));
        Assert.That(_viewModel.Theme, Is.EqualTo(_testConfig.Theme));
        Assert.That(_viewModel.AutoSave, Is.EqualTo(_testConfig.AutoSave));
        Assert.That(_viewModel.ServerUrl, Is.EqualTo(_testConfig.ServerUrl));
        Assert.That(_viewModel.Status, Contains.Substring("Configuration loaded"));
        
        _mockConfigurationService.Verify(x => x.LoadConfigurationAsync(), Times.AtLeast(1));
    }

    [Test]
    public async Task LoadConfigurationCommand_HandlesExceptionGracefully()
    {
        // Arrange
        _mockConfigurationService
            .Setup(x => x.LoadConfigurationAsync())
            .ThrowsAsync(new IOException("File not found"));
        
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);

        // Act
        if (_viewModel.LoadConfigurationCommand is IAsyncRelayCommand asyncCommand)
            await asyncCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.Status, Is.EqualTo("Failed to load configuration"));
    }

    [Test]
    public async Task SaveConfigurationCommand_SavesConfigurationSuccessfully()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        _viewModel.UserName = "Modified User";
        _viewModel.Theme = "Dark";
        _viewModel.AutoSave = false;
        _viewModel.ServerUrl = "https://modified.server.com";

        // Act
        if (_viewModel.SaveConfigurationCommand is IAsyncRelayCommand asyncCommand)
            await asyncCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.Status, Is.EqualTo("Configuration saved successfully"));
        
        _mockConfigurationService.Verify(x => x.SaveConfigurationAsync(
            It.Is<AppConfiguration>(config => 
                config.UserName == "Modified User" &&
                config.Theme == "Dark" &&
                config.AutoSave == false &&
                config.ServerUrl == "https://modified.server.com"
            )), Times.Once);
    }

    [Test]
    public async Task SaveConfigurationCommand_HandlesExceptionGracefully()
    {
        // Arrange
        _mockConfigurationService
            .Setup(x => x.SaveConfigurationAsync(It.IsAny<AppConfiguration>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));
        
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);

        // Act
        if (_viewModel.SaveConfigurationCommand is IAsyncRelayCommand asyncCommand)
            await asyncCommand.ExecuteAsync(null);

        // Assert
        Assert.That(_viewModel.Status, Is.EqualTo("Failed to save configuration"));
    }

    [Test]
    public void Properties_RaisePropertyChangedEvents()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        var propertyChangedEvents = new List<string>();
        
        _viewModel.PropertyChanged += (sender, args) => 
        {
            if (args.PropertyName != null)
                propertyChangedEvents.Add(args.PropertyName);
        };

        // Act
        _viewModel.UserName = "New User";
        _viewModel.Theme = "New Theme";
        _viewModel.AutoSave = !_viewModel.AutoSave; // Toggle to ensure change
        _viewModel.ServerUrl = "https://new.url.com";
        _viewModel.Status = "New Status";

        // Assert
        Assert.That(propertyChangedEvents, Contains.Item(nameof(_viewModel.UserName)));
        Assert.That(propertyChangedEvents, Contains.Item(nameof(_viewModel.Theme)));
        Assert.That(propertyChangedEvents, Contains.Item(nameof(_viewModel.AutoSave)));
        Assert.That(propertyChangedEvents, Contains.Item(nameof(_viewModel.ServerUrl)));
        Assert.That(propertyChangedEvents, Contains.Item(nameof(_viewModel.Status)));
    }

    [Test]
    public void UserName_PropertyGetterAndSetter_WorksCorrectly()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        const string testValue = "Test User Name";

        // Act
        _viewModel.UserName = testValue;

        // Assert
        Assert.That(_viewModel.UserName, Is.EqualTo(testValue));
    }

    [Test]
    public void Theme_PropertyGetterAndSetter_WorksCorrectly()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        const string testValue = "Dark Theme";

        // Act
        _viewModel.Theme = testValue;

        // Assert
        Assert.That(_viewModel.Theme, Is.EqualTo(testValue));
    }

    [Test]
    public void AutoSave_PropertyGetterAndSetter_WorksCorrectly()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        const bool testValue = true;

        // Act
        _viewModel.AutoSave = testValue;

        // Assert
        Assert.That(_viewModel.AutoSave, Is.EqualTo(testValue));
    }

    [Test]
    public void ServerUrl_PropertyGetterAndSetter_WorksCorrectly()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        const string testValue = "https://test.example.com";

        // Act
        _viewModel.ServerUrl = testValue;

        // Assert
        Assert.That(_viewModel.ServerUrl, Is.EqualTo(testValue));
    }

    [Test]
    public void Status_PropertyGetterAndSetter_WorksCorrectly()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        const string testValue = "Test Status Message";

        // Act
        _viewModel.Status = testValue;

        // Assert
        Assert.That(_viewModel.Status, Is.EqualTo(testValue));
    }

    [Test]
    public void ImplementsINotifyPropertyChanged()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);

        // Assert
        Assert.That(_viewModel, Is.InstanceOf<INotifyPropertyChanged>());
    }

    [Test]
    public async Task Constructor_LoadsConfigurationOnStartup()
    {
        // Arrange & Act
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);
        
        // Wait for async constructor load to complete
        await Task.Delay(100);

        // Assert
        _mockConfigurationService.Verify(x => x.LoadConfigurationAsync(), Times.AtLeastOnce);
    }

    [Test]
    public void Greeting_HasCorrectValue()
    {
        // Arrange
        _viewModel = new MainWindowViewModel(_mockConfigurationService.Object);

        // Act & Assert
        Assert.That(_viewModel.Greeting, Is.EqualTo("Welcome to Avalonia!"));
    }
}