using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Autofac;
using SimpleGui.ViewModels;
using SimpleGui.Views;
using SimpleGui.Services;
using Wrappers;

namespace SimpleGui;

public partial class App : Application
{
    private IContainer? _container;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var builder = new ContainerBuilder();

        // Register wrappers
        builder.RegisterType<FileWrapper>().As<IFileWrapper>().SingleInstance();

        // Register services
        builder.RegisterType<ConfigurationService>().As<IConfigurationService>().SingleInstance();

        // Register ViewModels
        builder.RegisterType<MainWindowViewModel>().AsSelf();

        _container = builder.Build();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            var viewModel = _container?.Resolve<MainWindowViewModel>() ?? new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Required for disabling Avalonia DataAnnotations validation plugin.")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}