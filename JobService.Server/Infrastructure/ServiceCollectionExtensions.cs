using JobService.Data;
using JobService.Repositories;
using JobService.Services;
using Microsoft.EntityFrameworkCore;

namespace JobService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure graceful shutdown
        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

        // Add gRPC services
        services.AddGrpc();

        // Add Entity Framework with SQLite configuration for better concurrency
        services.AddDbContext<JobContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        });

        // Add repositories
        services.AddScoped<IJobRepository, JobRepository>();

        // Add task management services
        services.AddSingleton<IJobTaskManager, JobTaskManager>();
        services.AddScoped<IJobTaskExecutor, RsyncJobTaskExecutor>();

        // Add infrastructure services
        services.AddSingleton<CancellationTokenSource>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}