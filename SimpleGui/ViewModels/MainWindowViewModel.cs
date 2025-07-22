using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGui.Models;
using SimpleGui.Services;

namespace SimpleGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
    
    [ObservableProperty]
    private string _userName = "";
    
    [ObservableProperty]
    private string _theme = "";
    
    [ObservableProperty]
    private bool _autoSave;
    
    [ObservableProperty]
    private string _serverUrl = "";
    
    [ObservableProperty]
    private string _status = "Ready";

    public MainWindowViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        LoadConfigurationCommand = new AsyncRelayCommand(LoadConfiguration);
        SaveConfigurationCommand = new AsyncRelayCommand(SaveConfiguration);
        
        // Load configuration on startup
        _ = Task.Run(LoadConfiguration);
    }

    public ICommand LoadConfigurationCommand { get; }
    public ICommand SaveConfigurationCommand { get; }

    private async Task LoadConfiguration()
    {
        try
        {
            Status = "Loading configuration...";
            var config = await _configurationService.LoadConfigurationAsync();
            
            UserName = config.UserName;
            Theme = config.Theme;
            AutoSave = config.AutoSave;
            ServerUrl = config.ServerUrl;
            
            Status = $"Configuration loaded (Last modified: {config.LastModified:yyyy-MM-dd HH:mm:ss})";
        }
        catch
        {
            Status = "Failed to load configuration";
        }
    }

    private async Task SaveConfiguration()
    {
        try
        {
            Status = "Saving configuration...";
            var config = new AppConfiguration
            {
                UserName = UserName,
                Theme = Theme,
                AutoSave = AutoSave,
                ServerUrl = ServerUrl
            };
            
            await _configurationService.SaveConfigurationAsync(config);
            Status = "Configuration saved successfully";
        }
        catch
        {
            Status = "Failed to save configuration";
        }
    }
}
