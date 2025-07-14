using JobService;
using JobService.Client;
using JobService.Grpc.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Google.Protobuf.WellKnownTypes;

namespace JobService.Client.Tests;

[TestFixture]
public class ApplicationTests
{
    private Mock<IJobServiceClient> _mockJobServiceClient;
    private Mock<ILogger<Application>> _mockLogger;
    private Application _application;

    [SetUp]
    public void SetUp()
    {
        _mockJobServiceClient = new Mock<IJobServiceClient>();
        _mockLogger = new Mock<ILogger<Application>>();
        _application = new Application(_mockJobServiceClient.Object, _mockLogger.Object);
    }

    [Test]
    public async Task RunAsync_WhenSuccessfulFlow_CompletesAllTestsSuccessfully()
    {
        // Arrange
        var job = CreateSampleJob();
        var createResponse = CreateJobResponse(true, "Job created successfully", job);
        var getAllResponse = CreateGetAllJobsResponse(true, "Jobs retrieved successfully", new List<Job> { job });
        var getResponse = CreateJobResponse(true, "Job retrieved successfully", job);
        var updateResponse = CreateJobResponse(true, "Job updated successfully", job);
        var deleteResponse = CreateDeleteJobResponse(true, "Job deleted successfully");

        SetupSuccessfulFlow(createResponse, getAllResponse, getResponse, updateResponse, deleteResponse);

        // Act
        await _application.RunAsync();

        // Assert
        VerifyAllOperationsCalled();
        VerifySuccessLogging();
    }

    [Test]
    public async Task RunAsync_WhenNoJobsExist_SkipsJobSpecificTests()
    {
        // Arrange
        var createResponse = CreateJobResponse(true, "Job created successfully", CreateSampleJob());
        var emptyGetAllResponse = CreateGetAllJobsResponse(true, "No jobs found", new List<Job>());

        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .ReturnsAsync(createResponse);
        _mockJobServiceClient.Setup(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()))
            .ReturnsAsync(emptyGetAllResponse);

        // Act
        await _application.RunAsync();

        // Assert
        _mockJobServiceClient.Verify(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()), Times.Once);
        _mockJobServiceClient.Verify(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()), Times.Once);

        // Verify job-specific operations are not called
        _mockJobServiceClient.Verify(x => x.GetJobAsync(It.IsAny<GetJobRequest>()), Times.Never);
        _mockJobServiceClient.Verify(x => x.UpdateJobAsync(It.IsAny<UpdateJobRequest>()), Times.Never);
        _mockJobServiceClient.Verify(x => x.DeleteJobAsync(It.IsAny<DeleteJobRequest>()), Times.Never);
    }

    [Test]
    public void RunAsync_WhenCreateJobThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var exception = new Exception("Connection failed");
        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var actualException = Assert.ThrowsAsync<Exception>(async () => await _application.RunAsync());
        Assert.That(actualException, Is.EqualTo(exception));

        VerifyErrorLogging(exception);
        VerifyConnectionTroubleshootingLogged();
    }

    [Test]
    public void RunAsync_WhenGetAllJobsThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var job = CreateSampleJob();
        var createResponse = CreateJobResponse(true, "Job created successfully", job);
        var exception = new Exception("Network error");

        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .ReturnsAsync(createResponse);
        _mockJobServiceClient.Setup(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var actualException = Assert.ThrowsAsync<Exception>(async () => await _application.RunAsync());
        Assert.That(actualException, Is.EqualTo(exception));

        VerifyErrorLogging(exception);
    }

    [Test]
    public async Task RunAsync_CreateJobRequest_HasCorrectProperties()
    {
        // Arrange
        CreateJobRequest? capturedRequest = null;
        var createResponse = CreateJobResponse(true, "Job created successfully", CreateSampleJob());
        var emptyGetAllResponse = CreateGetAllJobsResponse(true, "No jobs found", new List<Job>());

        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .Callback<CreateJobRequest>(req => capturedRequest = req)
            .ReturnsAsync(createResponse);
        _mockJobServiceClient.Setup(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()))
            .ReturnsAsync(emptyGetAllResponse);

        // Act
        await _application.RunAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest.Name, Is.EqualTo("Test Job"));
        Assert.That(capturedRequest.WorkDir, Is.EqualTo("/tmp/test"));
        Assert.That(capturedRequest.ClusterName, Is.EqualTo("test-cluster"));
    }

    [Test]
    public async Task RunAsync_UpdateJobRequest_HasCorrectProperties()
    {
        // Arrange
        var job = CreateSampleJob();
        var createResponse = CreateJobResponse(true, "Job created successfully", job);
        var getAllResponse = CreateGetAllJobsResponse(true, "Jobs retrieved successfully", new List<Job> { job });
        var getResponse = CreateJobResponse(true, "Job retrieved successfully", job);
        var deleteResponse = CreateDeleteJobResponse(true, "Job deleted successfully");

        UpdateJobRequest? capturedRequest = null;
        var updateResponse = CreateJobResponse(true, "Job updated successfully", job);

        SetupBasicFlow(createResponse, getAllResponse, getResponse);
        _mockJobServiceClient.Setup(x => x.UpdateJobAsync(It.IsAny<UpdateJobRequest>()))
            .Callback<UpdateJobRequest>(req => capturedRequest = req)
            .ReturnsAsync(updateResponse);
        _mockJobServiceClient.Setup(x => x.DeleteJobAsync(It.IsAny<DeleteJobRequest>()))
            .ReturnsAsync(deleteResponse);

        // Act
        await _application.RunAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest.Id, Is.EqualTo(job.Id));
        Assert.That(capturedRequest.Name, Is.EqualTo("Updated Test Job"));
        Assert.That(capturedRequest.WorkDir, Is.EqualTo("/tmp/updated"));
        Assert.That(capturedRequest.ClusterName, Is.EqualTo("updated-cluster"));
    }

    [Test]
    public async Task RunAsync_LogsOperationResults()
    {
        // Arrange
        var job = CreateSampleJob();
        var createResponse = CreateJobResponse(true, "Job created successfully", job);
        var getAllResponse = CreateGetAllJobsResponse(true, "Jobs retrieved successfully", new List<Job> { job });
        var getResponse = CreateJobResponse(true, "Job retrieved successfully", job);
        var updateResponse = CreateJobResponse(true, "Job updated successfully", job);
        var deleteResponse = CreateDeleteJobResponse(true, "Job deleted successfully");

        SetupSuccessfulFlow(createResponse, getAllResponse, getResponse, updateResponse, deleteResponse);

        // Act
        await _application.RunAsync();

        // Assert
        VerifyLogMessage(LogLevel.Information, "1. Creating a job...");
        VerifyLogMessage(LogLevel.Information, "2. Getting all jobs...");
        VerifyLogMessage(LogLevel.Information, "✅ All tests completed successfully!");
    }

    private void SetupSuccessfulFlow(JobResponse createResponse, GetAllJobsResponse getAllResponse,
        JobResponse getResponse, JobResponse updateResponse, DeleteJobResponse deleteResponse)
    {
        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .ReturnsAsync(createResponse);
        _mockJobServiceClient.Setup(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()))
            .ReturnsAsync(getAllResponse);
        _mockJobServiceClient.Setup(x => x.GetJobAsync(It.IsAny<GetJobRequest>()))
            .ReturnsAsync(getResponse);
        _mockJobServiceClient.Setup(x => x.UpdateJobAsync(It.IsAny<UpdateJobRequest>()))
            .ReturnsAsync(updateResponse);
        _mockJobServiceClient.Setup(x => x.DeleteJobAsync(It.IsAny<DeleteJobRequest>()))
            .ReturnsAsync(deleteResponse);
    }

    private void SetupBasicFlow(JobResponse createResponse, GetAllJobsResponse getAllResponse, JobResponse getResponse)
    {
        _mockJobServiceClient.Setup(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()))
            .ReturnsAsync(createResponse);
        _mockJobServiceClient.Setup(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()))
            .ReturnsAsync(getAllResponse);
        _mockJobServiceClient.Setup(x => x.GetJobAsync(It.IsAny<GetJobRequest>()))
            .ReturnsAsync(getResponse);
    }

    private void VerifyAllOperationsCalled()
    {
        _mockJobServiceClient.Verify(x => x.CreateJobAsync(It.IsAny<CreateJobRequest>()), Times.Exactly(2)); // Initial + Second job
        _mockJobServiceClient.Verify(x => x.GetAllJobsAsync(It.IsAny<GetAllJobsRequest>()), Times.Exactly(3)); // Initial + After second job + Final
        _mockJobServiceClient.Verify(x => x.GetJobAsync(It.IsAny<GetJobRequest>()), Times.Once);
        _mockJobServiceClient.Verify(x => x.UpdateJobAsync(It.IsAny<UpdateJobRequest>()), Times.Once);
        _mockJobServiceClient.Verify(x => x.DeleteJobAsync(It.IsAny<DeleteJobRequest>()), Times.Once);
    }

    private void VerifySuccessLogging()
    {
        VerifyLogMessage(LogLevel.Information, "✅ All tests completed successfully!");
    }

    private void VerifyErrorLogging(Exception exception)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("❌ Error during testing")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyConnectionTroubleshootingLogged()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Make sure the JobService server is running")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyLogMessage(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static Job CreateSampleJob()
    {
        return new Job
        {
            Id = 1,
            Name = "Test Job",
            WorkDir = "/tmp/test",
            ClusterName = "test-cluster",
            CreatedAt = Timestamp.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc))
        };
    }

    private static JobResponse CreateJobResponse(bool success, string message, Job job)
    {
        return new JobResponse
        {
            Success = success,
            Message = message,
            Job = job
        };
    }

    private static GetAllJobsResponse CreateGetAllJobsResponse(bool success, string message, IList<Job> jobs)
    {
        var response = new GetAllJobsResponse
        {
            Success = success,
            Message = message
        };

        foreach (var job in jobs)
        {
            response.Jobs.Add(job);
        }

        return response;
    }

    private static DeleteJobResponse CreateDeleteJobResponse(bool success, string message)
    {
        return new DeleteJobResponse
        {
            Success = success,
            Message = message
        };
    }
}