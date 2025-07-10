using Grpc.Net.Client;
using JobService.Client.Interfaces;
using JobService.Client.Services;
using JobService.Common;
using Microsoft.Extensions.Logging;

namespace JobService.IntegrationTests.Fixtures;

public class ClientTestFixture : IAsyncDisposable
{
    private readonly ConnectionConfiguration _connectionConfig;
    private readonly ILogger<GrpcChannelFactory> _logger;
    private GrpcChannel? _channel;
    private global::JobService.JobService.JobServiceClient? _grpcClient;

    public global::JobService.JobService.JobServiceClient GrpcClient => _grpcClient ?? throw new InvalidOperationException("Client not initialized");

    public ClientTestFixture(ConnectionConfiguration connectionConfig)
    {
        _connectionConfig = connectionConfig;
        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<GrpcChannelFactory>();
    }

    public async Task InitializeAsync()
    {
        var channelFactory = new GrpcChannelFactory(_logger, _connectionConfig);
        _channel = channelFactory.CreateChannel();
        _grpcClient = new global::JobService.JobService.JobServiceClient(_channel);
        
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
                _logger.LogWarning($"Connection attempt {i + 1} failed: {ex.Message}");
                if (i == maxRetries - 1)
                    throw new InvalidOperationException($"Failed to connect to server after {maxRetries} attempts", ex);
                await Task.Delay(500);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.ShutdownAsync();
            _channel.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}