using NUnit.Framework;
using JobService.IntegrationTests.Fixtures;
using JobService.IntegrationTests.Helpers;
using JobService;
using JobService.Common;
using Grpc.Core;

namespace JobService.IntegrationTests.Tests;

[TestFixture]
[Timeout(30000)] // 30 second timeout for all tests
public class CrudOperationsTests
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
    public async Task CreateJob_ShouldReturnValidJobResponse()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidJobRequest();

        // Act
        var response = await _clientFixture.GrpcClient.CreateJobAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.Job);
        Assert.IsTrue(response.Job.Id > 0);
        Assert.AreEqual(request.Name, response.Job.Name);
        Assert.AreEqual(request.WorkDir, response.Job.WorkDir);
        Assert.AreEqual(request.ClusterName, response.Job.ClusterName);
        Assert.IsTrue(response.Job.CreatedAt.ToDateTime() > DateTime.UtcNow.AddMinutes(-1));
    }

    [Test]
    public async Task GetJob_ShouldReturnCorrectJob()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture.GrpcClient.CreateJobAsync(createRequest);
        var getRequest = TestDataBuilder.CreateGetJobRequest(createResponse.Job.Id);

        // Act
        var response = await _clientFixture.GrpcClient.GetJobAsync(getRequest);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.Job);
        Assert.AreEqual(createResponse.Job.Id, response.Job.Id);
        Assert.AreEqual(createResponse.Job.Name, response.Job.Name);
        Assert.AreEqual(createResponse.Job.WorkDir, response.Job.WorkDir);
        Assert.AreEqual(createResponse.Job.ClusterName, response.Job.ClusterName);
    }

    [Test]
    public async Task UpdateJob_ShouldUpdateJobCorrectly()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture.GrpcClient.CreateJobAsync(createRequest);
        var updateRequest = TestDataBuilder.CreateUpdateJobRequest(createResponse.Job.Id);

        // Act
        var response = await _clientFixture.GrpcClient.UpdateJobAsync(updateRequest);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.Job);
        Assert.AreEqual(updateRequest.Id, response.Job.Id);
        Assert.AreEqual(updateRequest.Name, response.Job.Name);
        Assert.AreEqual(updateRequest.WorkDir, response.Job.WorkDir);
        Assert.AreEqual(updateRequest.ClusterName, response.Job.ClusterName);
    }

    [Test]
    public async Task DeleteJob_ShouldRemoveJobSuccessfully()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture.GrpcClient.CreateJobAsync(createRequest);
        var deleteRequest = TestDataBuilder.CreateDeleteJobRequest(createResponse.Job.Id);

        // Act
        var deleteResponse = await _clientFixture.GrpcClient.DeleteJobAsync(deleteRequest);

        // Assert
        Assert.IsNotNull(deleteResponse);
        Assert.IsTrue(deleteResponse.Success);

        // Verify job is deleted
        var getRequest = TestDataBuilder.CreateGetJobRequest(createResponse.Job.Id);
        Assert.ThrowsAsync<RpcException>(async () => await _clientFixture.GrpcClient.GetJobAsync(getRequest));
    }

    [Test]
    public async Task GetAllJobs_ShouldReturnAllCreatedJobs()
    {
        // Arrange
        var job1 = TestDataBuilder.CreateValidJobRequest();
        var job2 = TestDataBuilder.CreateValidJobRequest();
        
        await _clientFixture.GrpcClient.CreateJobAsync(job1);
        await _clientFixture.GrpcClient.CreateJobAsync(job2);

        var getAllRequest = TestDataBuilder.CreateGetAllJobsRequest();

        // Act
        var response = await _clientFixture.GrpcClient.GetAllJobsAsync(getAllRequest);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Success);
        Assert.IsTrue(response.Jobs.Count >= 2);
        Assert.That(response.Jobs, Has.Some.Property("Name").EqualTo(job1.Name));
        Assert.That(response.Jobs, Has.Some.Property("Name").EqualTo(job2.Name));
    }

    [Test]
    public async Task CompleteWorkflow_ShouldExecuteAllCrudOperations()
    {
        // Create
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture.GrpcClient.CreateJobAsync(createRequest);
        Assert.IsTrue(createResponse.Job.Id > 0);

        // Read
        var getRequest = TestDataBuilder.CreateGetJobRequest(createResponse.Job.Id);
        var getResponse = await _clientFixture.GrpcClient.GetJobAsync(getRequest);
        Assert.AreEqual(createResponse.Job.Id, getResponse.Job.Id);

        // Update
        var updateRequest = TestDataBuilder.CreateUpdateJobRequest(createResponse.Job.Id);
        var updateResponse = await _clientFixture.GrpcClient.UpdateJobAsync(updateRequest);
        Assert.AreEqual(updateRequest.Name, updateResponse.Job.Name);

        // Delete
        var deleteRequest = TestDataBuilder.CreateDeleteJobRequest(createResponse.Job.Id);
        var deleteResponse = await _clientFixture.GrpcClient.DeleteJobAsync(deleteRequest);
        Assert.IsTrue(deleteResponse.Success);
    }
}