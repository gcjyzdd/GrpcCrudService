using Autofac;
using JobService.ProgressTest;
using Serilog;

namespace JobService.ProgressTest;

static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var container = ApplicationFactory.CreateContainer();
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
            await Log.CloseAndFlushAsync();
        }
    }
}