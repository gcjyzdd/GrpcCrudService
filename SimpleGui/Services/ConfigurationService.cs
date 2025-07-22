using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SimpleGui.Models;
using Wrappers;

namespace SimpleGui.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IFileWrapper _fileWrapper;
    private readonly string _configFilePath;

    public ConfigurationService(IFileWrapper fileWrapper)
    {
        _fileWrapper = fileWrapper;
        _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SimpleGui",
            "config.json"
        );
    }

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (!_fileWrapper.Exists(_configFilePath))
            {
                var defaultConfig = GetDefaultConfiguration();
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await _fileWrapper.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize(json, AppConfigurationContext.Default.AppConfiguration);
            
            return config ?? GetDefaultConfiguration();
        }
        catch
        {
            return GetDefaultConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(AppConfiguration configuration)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            configuration.LastModified = DateTime.Now;
            
            var json = JsonSerializer.Serialize(configuration, AppConfigurationContext.Default.AppConfiguration);
            await _fileWrapper.WriteAllTextAsync(_configFilePath, json);
        }
        catch
        {
            // Silently handle save errors for demo purposes
            // In production, you might want to log or notify the user
        }
    }

    public AppConfiguration GetDefaultConfiguration()
    {
        return new AppConfiguration();
    }
}