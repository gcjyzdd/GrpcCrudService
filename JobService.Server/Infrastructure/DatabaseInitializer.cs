using JobService.Data;
using Microsoft.EntityFrameworkCore;

namespace JobService.Infrastructure;

public class DatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobContext>();
        
        try
        {
            await context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Database ensured created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while ensuring the database was created.");
            throw;
        }
    }
}