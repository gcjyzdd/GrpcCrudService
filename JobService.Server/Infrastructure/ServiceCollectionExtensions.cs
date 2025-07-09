using JobService.Data;
using JobService.Repositories;
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

        // Add Entity Framework
        services.AddDbContext<JobContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Add repositories
        services.AddScoped<IJobRepository, JobRepository>();

        // Add infrastructure services
        services.AddSingleton<CancellationTokenSource>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}