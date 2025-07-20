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
        // Add overall timeout to prevent hanging indefinitely
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        try
        {
            await InitializeAsyncInternal(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException("Client initialization timed out after 30 seconds");
        }
    }

    private async Task InitializeAsyncInternal(CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var testRequest = new GetAllJobsRequest();
                var call = _grpcClient.GetAllJobsAsync(testRequest);
                
                // Add timeout to each gRPC call
                using var callTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, callTimeoutCts.Token);
                
                await call.ResponseAsync.WaitAsync(combinedCts.Token);
                return; // Success!
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Re-throw the main cancellation
            }
            catch (Exception ex)
            {
                var logger = _scope.Resolve<ILogger<ClientTestFixture>>();
                logger.LogWarning($"Connection attempt {i + 1} failed: {ex.Message}");
                if (i == maxRetries - 1)
                    throw new InvalidOperationException($"Failed to connect to server after {maxRetries} attempts", ex);
                
                try
                {
                    await Task.Delay(500, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // Re-throw if main timeout was hit
                }
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