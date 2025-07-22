using System.Threading.Tasks;
using SimpleGui.Models;

namespace SimpleGui.Services;

public interface IConfigurationService
{
    Task<AppConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(AppConfiguration configuration);
    AppConfiguration GetDefaultConfiguration();
}