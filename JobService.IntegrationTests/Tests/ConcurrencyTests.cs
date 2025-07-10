using NUnit.Framework;
using JobService.IntegrationTests.Fixtures;
using JobService.IntegrationTests.Helpers;
using JobService;
using JobService.Common;

namespace JobService.IntegrationTests.Tests;

[TestFixture]
public class ConcurrencyTests
{
    private ServerTestFixture _serverFixture = null!;
    private ClientTestFixture _clientFixture1 = null!;
    private ClientTestFixture _clientFixture2 = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var connectionConfig = TestConnectionGenerator.GenerateRandomPaths();
        _serverFixture = new ServerTestFixture(connectionConfig);
        _clientFixture1 = new ClientTestFixture(connectionConfig);
        _clientFixture2 = new ClientTestFixture(connectionConfig);
        
        await _serverFixture.StartAsync();
        await _clientFixture1.InitializeAsync();
        await _clientFixture2.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _clientFixture1.DisposeAsync();
        await _clientFixture2.DisposeAsync();
        await _serverFixture.DisposeAsync();
    }

    [Test]
    public async Task MultipleClients_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<JobResponse>>();
        var jobRequests = new List<CreateJobRequest>();

        for (int i = 0; i < 10; i++)
        {
            var request = TestDataBuilder.CreateValidJobRequest($"ConcurrentJob_{i}");
            jobRequests.Add(request);
            
            var client = i % 2 == 0 ? _clientFixture1.GrpcClient : _clientFixture2.GrpcClient;
            tasks.Add(client.CreateJobAsync(request).ResponseAsync);
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, responses.Length);
        Assert.That(responses, Has.All.Property("Success").True);
        Assert.That(responses, Has.All.Property("Job").Not.Null);
        Assert.That(responses.Select(r => r.Job.Id), Has.All.GreaterThan(0));
        
        // Verify all jobs have unique IDs
        var ids = responses.Select(r => r.Job.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }

    [Test]
    public async Task ConcurrentReadOperations_ShouldReturnConsistentResults()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture1.GrpcClient.CreateJobAsync(createRequest);
        var getRequest = TestDataBuilder.CreateGetJobRequest(createResponse.Job.Id);

        // Act
        var readTasks = new List<Task<JobResponse>>();
        for (int i = 0; i < 20; i++)
        {
            var client = i % 2 == 0 ? _clientFixture1.GrpcClient : _clientFixture2.GrpcClient;
            readTasks.Add(client.GetJobAsync(getRequest).ResponseAsync);
        }

        var responses = await Task.WhenAll(readTasks);

        // Assert
        Assert.That(responses, Has.All.Property("Success").True);
        Assert.That(responses, Has.All.Property("Job").Not.Null);
        
        foreach (var response in responses)
        {
            Assert.AreEqual(createResponse.Job.Id, response.Job.Id);
            Assert.AreEqual(createResponse.Job.Name, response.Job.Name);
            Assert.AreEqual(createResponse.Job.WorkDir, response.Job.WorkDir);
            Assert.AreEqual(createResponse.Job.ClusterName, response.Job.ClusterName);
        }
    }

    [Test]
    public async Task ConcurrentUpdateOperations_ShouldHandleRaceConditions()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateValidJobRequest();
        var createResponse = await _clientFixture1.GrpcClient.CreateJobAsync(createRequest);

        // Act
        var updateTasks = new List<Task<JobResponse>>();
        for (int i = 0; i < 5; i++)
        {
            var updateRequest = TestDataBuilder.CreateUpdateJobRequest(createResponse.Job.Id, $"UpdatedName_{i}");
            var client = i % 2 == 0 ? _clientFixture1.GrpcClient : _clientFixture2.GrpcClient;
            updateTasks.Add(client.UpdateJobAsync(updateRequest).ResponseAsync);
        }

        var responses = await Task.WhenAll(updateTasks);

        // Assert
        Assert.AreEqual(5, responses.Length);
        Assert.That(responses, Has.All.Property("Success").True);
        Assert.That(responses, Has.All.Property("Job").Not.Null);
        Assert.That(responses.Select(r => r.Job.Id), Has.All.EqualTo(createResponse.Job.Id));
        
        // Verify final state
        var getRequest = TestDataBuilder.CreateGetJobRequest(createResponse.Job.Id);
        var finalState = await _clientFixture1.GrpcClient.GetJobAsync(getRequest);
        Assert.IsTrue(finalState.Job.Name.StartsWith("UpdatedName_"));
    }
}