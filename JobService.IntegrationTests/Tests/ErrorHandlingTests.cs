using Grpc.Core;
using NUnit.Framework;
using JobService.IntegrationTests.Fixtures;
using JobService.IntegrationTests.Helpers;
using JobService;
using JobService.Common;

namespace JobService.IntegrationTests.Tests;

[TestFixture]
public class ErrorHandlingTests
{
    private ServerTestFixture _serverFixture = null!;
    private ClientTestFixture _clientFixture = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var connectionConfig = TestConnectionGenerator.GenerateRandomPaths();
        _serverFixture = new ServerTestFixture(connectionConfig);
        _clientFixture = new ClientTestFixture(connectionConfig);
        
        await _serverFixture.StartAsync();
        await _clientFixture.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _clientFixture.DisposeAsync();
        await _serverFixture.DisposeAsync();
    }

    [Test]
    public async Task GetJob_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var getRequest = TestDataBuilder.CreateGetJobRequest(99999);

        // Act & Assert
        Assert.ThrowsAsync<RpcException>(async () => await _clientFixture.GrpcClient.GetJobAsync(getRequest));
    }

    [Test]
    public async Task UpdateJob_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var updateRequest = TestDataBuilder.CreateUpdateJobRequest(99999);

        // Act & Assert
        Assert.ThrowsAsync<RpcException>(async () => await _clientFixture.GrpcClient.UpdateJobAsync(updateRequest));
    }

    [Test]
    public async Task DeleteJob_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var deleteRequest = TestDataBuilder.CreateDeleteJobRequest(99999);

        // Act & Assert
        Assert.ThrowsAsync<RpcException>(async () => await _clientFixture.GrpcClient.DeleteJobAsync(deleteRequest));
    }

    [Test]
    public async Task CreateJob_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        var createRequest = new CreateJobRequest
        {
            Name = "",  // Invalid empty name
            WorkDir = "/tmp/test",
            ClusterName = "test-cluster"
        };

        // Act & Assert
        Assert.ThrowsAsync<RpcException>(async () => await _clientFixture.GrpcClient.CreateJobAsync(createRequest));
    }

    [Test]
    public async Task MultipleOperations_WithServerRestart_ShouldHandleGracefully()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture.GrpcClient.CreateJobAsync(createRequest);

        // Act - Simulate server restart
        await _serverFixture.StopAsync();
        await Task.Delay(1000); // Wait for shutdown
        await _serverFixture.StartAsync();
        await Task.Delay(1000); // Wait for startup

        // Create a new client after server restart
        await _clientFixture.DisposeAsync();
        var newClientFixture = new ClientTestFixture(_serverFixture.ConnectionConfig);
        await newClientFixture.InitializeAsync();

        // Assert - Should be able to perform operations after restart
        var getAllRequest = TestDataBuilder.CreateGetAllJobsRequest();
        var response = await newClientFixture.GrpcClient.GetAllJobsAsync(getAllRequest);
        
        Assert.IsNotNull(response);
        // Note: Data might be lost after restart depending on database configuration
        
        await newClientFixture.DisposeAsync();
    }
}