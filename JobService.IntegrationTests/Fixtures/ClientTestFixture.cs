using Autofac;
using JobService.Client;
using JobService.Grpc.Interfaces;
using JobService.Common;
using Microsoft.Extensions.Logging;

namespace JobService.IntegrationTests.Fixtures;

public class ClientTestFixture : IAsyncDisposable
{
    private readonly ConnectionConfiguration _connectionConfig;
    private IContainer? _container;
    private ILifetimeScope? _scope;
    private global::JobService.JobService.JobServiceClient? _grpcClient;

    public global::JobService.JobService.JobServiceClient GrpcClient => _grpcClient ?? throw new InvalidOperationException("Client not initialized");

    public ClientTestFixture(ConnectionConfiguration connectionConfig)
    {
        _connectionConfig = connectionConfig;
    }

    public async Task InitializeAsync()
    {
        // Create client container using ApplicationFactory
        _container = ApplicationFactory.CreateContainer(_connectionConfig);
        _scope = _container.BeginLifetimeScope();
        
        // Get the gRPC client from the container
        _grpcClient = _scope.Resolve<global::JobService.JobService.JobServiceClient>();

        // Test the connection by trying to get all jobs (which should work even if empty)
        var maxRetries = 10;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var testRequest = new GetAllJobsRequest();
                await _grpcClient.GetAllJobsAsync(testRequest);
                return; // Success!
            }
            catch (Exception ex)
            {
                var logger = _scope.Resolve<ILogger<ClientTestFixture>>();
                logger.LogWarning($"Connection attempt {i + 1} failed: {ex.Message}");
                if (i == maxRetries - 1)
                    throw new InvalidOperationException($"Failed to connect to server after {maxRetries} attempts", ex);
                await Task.Delay(500);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _scope?.Dispose();
        _container?.Dispose();

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}