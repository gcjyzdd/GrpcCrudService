using Autofac;
using Autofac.Extensions.DependencyInjection;
using JobService.Client.Interfaces;
using JobService.Client.Services;
using JobService.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace JobService.Client;

public static class ApplicationFactory
{
    public static IContainer CreateContainer(IConnectionConfiguration? connectionConfig = null)
    {
        return ConfigureServices(connectionConfig);
    }

    private static IContainer ConfigureServices(IConnectionConfiguration? connectionConfig)
    {
        var builder = new ContainerBuilder();

        ConfigureSerilog();
        RegisterLogging(builder);
        RegisterConnectionConfiguration(builder, connectionConfig);
        RegisterGrpcServices(builder);
        RegisterApplication(builder);

        return builder.Build();
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/client-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void RegisterLogging(ContainerBuilder builder)
    {
        builder.RegisterInstance(Log.Logger).As<Serilog.ILogger>();
        builder.Register<ILoggerFactory>(context => new SerilogLoggerFactory(Log.Logger))
            .As<ILoggerFactory>()
            .SingleInstance();
        builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
    }

    private static void RegisterConnectionConfiguration(ContainerBuilder builder, IConnectionConfiguration? connectionConfig)
    {
        var config = connectionConfig ?? new ConnectionConfiguration();
        builder.RegisterInstance(config)
            .As<IConnectionConfiguration>()
            .SingleInstance();
    }

    private static void RegisterGrpcServices(ContainerBuilder builder)
    {
        builder.RegisterType<GrpcChannelFactory>()
            .As<IGrpcChannelFactory>()
            .SingleInstance();

        builder.Register(context =>
        {
            var channelFactory = context.Resolve<IGrpcChannelFactory>();
            var channel = channelFactory.CreateChannel();
            return new global::JobService.JobService.JobServiceClient(channel);
        }).SingleInstance();

        builder.RegisterType<JobServiceClient>()
            .As<IJobServiceClient>()
            .SingleInstance();
    }

    private static void RegisterApplication(ContainerBuilder builder)
    {
        builder.RegisterType<Application>()
            .AsSelf()
            .SingleInstance();
    }
}