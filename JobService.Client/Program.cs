using Autofac;
using Autofac.Extensions.DependencyInjection;
using JobService;
using JobService.Client;
using JobService.Client.Interfaces;
using JobService.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

class Program
{
    public static async Task Main(string[] args)
    {
        var container = ConfigureServices();
        
        try
        {
            using var scope = container.BeginLifetimeScope();
            var app = scope.Resolve<Application>();
            
            await app.RunAsync();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IContainer ConfigureServices()
    {
        var builder = new ContainerBuilder();
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/client-.log", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        // Register logging
        builder.RegisterInstance(Log.Logger).As<Serilog.ILogger>();
        builder.Register<ILoggerFactory>(context => new SerilogLoggerFactory(Log.Logger))
            .As<ILoggerFactory>()
            .SingleInstance();
        builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));

        // Register gRPC channel factory
        builder.RegisterType<GrpcChannelFactory>()
            .As<IGrpcChannelFactory>()
            .SingleInstance();

        // Register gRPC client
        builder.Register(context =>
        {
            var channelFactory = context.Resolve<IGrpcChannelFactory>();
            var channel = channelFactory.CreateChannel();
            return new global::JobService.JobService.JobServiceClient(channel);
        }).SingleInstance();

        // Register job service client
        builder.RegisterType<JobServiceClient>()
            .As<IJobServiceClient>()
            .SingleInstance();

        // Register application
        builder.RegisterType<Application>()
            .AsSelf()
            .SingleInstance();

        return builder.Build();
    }
}