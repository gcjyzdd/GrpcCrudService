using JobService.Infrastructure;

namespace JobService.Server;

public static class ApplicationFactory
{
    public static Application CreateApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        ConfigureServices(builder);
        
        return new Application(builder);
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddJobServiceInfrastructure(builder.Configuration);
    }
}