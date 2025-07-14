using JobService;
using JobService.Grpc.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobService.Client;

public class Application
{
    private readonly IJobServiceClient _jobServiceClient;
    private readonly ILogger<Application> _logger;

    public Application(IJobServiceClient jobServiceClient, ILogger<Application> logger)
    {
        _jobServiceClient = jobServiceClient;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("JobService gRPC Client - Testing CRUD Operations");
        _logger.LogInformation("================================================");

        try
        {
            await TestCreateJob();
            var jobs = await TestGetAllJobs();

            if (jobs.Count > 0)
            {
                var firstJob = jobs[0];
                await TestGetSpecificJob(firstJob.Id);
                await TestUpdateJob(firstJob.Id);
                await TestCreateSecondJob();
                await TestGetAllJobsAgain();
                await TestDeleteJob(firstJob.Id);
                await TestFinalGetAllJobs();
            }

            _logger.LogInformation("✅ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during testing");
            LogConnectionTroubleshooting();
            throw;
        }
    }

    private async Task<JobResponse> TestCreateJob()
    {
        _logger.LogInformation("1. Creating a job...");
        var createResponse = await _jobServiceClient.CreateJobAsync(new CreateJobRequest
        {
            Name = "Test Job",
            WorkDir = "/tmp/test",
            ClusterName = "test-cluster"
        });

        LogJobOperationResult(createResponse);
        LogJobDetails(createResponse.Job, "Created");
        return createResponse;
    }

    private async Task<IList<Job>> TestGetAllJobs()
    {
        _logger.LogInformation("2. Getting all jobs...");
        var getAllResponse = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());

        _logger.LogInformation("   Success: {Success}", getAllResponse.Success);
        _logger.LogInformation("   Message: {Message}", getAllResponse.Message);
        _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse.Jobs.Count);

        LogJobList(getAllResponse.Jobs);
        return getAllResponse.Jobs;
    }

    private async Task TestGetSpecificJob(int jobId)
    {
        _logger.LogInformation("3. Getting specific job...");
        var getResponse = await _jobServiceClient.GetJobAsync(new GetJobRequest { Id = jobId });

        LogJobOperationResult(getResponse);
        if (getResponse.Success)
        {
            LogJobDetails(getResponse.Job, "Retrieved");
            _logger.LogInformation("   Created: {CreatedAt}", getResponse.Job.CreatedAt);
        }
    }

    private async Task TestUpdateJob(int jobId)
    {
        _logger.LogInformation("4. Updating job...");
        var updateResponse = await _jobServiceClient.UpdateJobAsync(new UpdateJobRequest
        {
            Id = jobId,
            Name = "Updated Test Job",
            WorkDir = "/tmp/updated",
            ClusterName = "updated-cluster"
        });

        LogJobOperationResult(updateResponse);
        LogJobDetails(updateResponse.Job, "Updated");
    }

    private async Task TestCreateSecondJob()
    {
        _logger.LogInformation("5. Creating another job...");
        var createResponse2 = await _jobServiceClient.CreateJobAsync(new CreateJobRequest
        {
            Name = "Second Job",
            WorkDir = "/tmp/second",
            ClusterName = "second-cluster"
        });

        LogJobOperationResult(createResponse2);
    }

    private async Task TestGetAllJobsAgain()
    {
        _logger.LogInformation("6. Getting all jobs again...");
        var getAllResponse2 = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());

        _logger.LogInformation("   Success: {Success}", getAllResponse2.Success);
        _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse2.Jobs.Count);

        LogJobList(getAllResponse2.Jobs);
    }

    private async Task TestDeleteJob(int jobId)
    {
        _logger.LogInformation("7. Deleting first job...");
        var deleteResponse = await _jobServiceClient.DeleteJobAsync(new DeleteJobRequest { Id = jobId });

        _logger.LogInformation("   Success: {Success}", deleteResponse.Success);
        _logger.LogInformation("   Message: {Message}", deleteResponse.Message);
    }

    private async Task TestFinalGetAllJobs()
    {
        _logger.LogInformation("8. Final check - getting all jobs...");
        var getAllResponse3 = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());

        _logger.LogInformation("   Success: {Success}", getAllResponse3.Success);
        _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse3.Jobs.Count);

        LogJobList(getAllResponse3.Jobs);
    }

    private void LogJobOperationResult(JobResponse response)
    {
        _logger.LogInformation("   Success: {Success}", response.Success);
        _logger.LogInformation("   Message: {Message}", response.Message);
    }

    private void LogJobDetails(Job job, string operation)
    {
        if (job != null)
        {
            _logger.LogInformation("   {Operation} Job ID: {JobId}", operation, job.Id);
            _logger.LogInformation("   Job Name: {JobName}", job.Name);
            _logger.LogInformation("   Work Dir: {WorkDir}", job.WorkDir);
            _logger.LogInformation("   Cluster: {ClusterName}", job.ClusterName);
        }
    }

    private void LogJobList(IEnumerable<Job> jobs)
    {
        foreach (var job in jobs)
        {
            _logger.LogInformation("   - Job {JobId}: {JobName} ({ClusterName})", job.Id, job.Name, job.ClusterName);
        }
    }

    private void LogConnectionTroubleshooting()
    {
        if (OperatingSystem.IsWindows())
        {
            _logger.LogError("Make sure the JobService server is running with named pipe: JobServicePipe");
        }
        else
        {
            var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
            _logger.LogError("Make sure the JobService server is running with Unix socket: {SocketPath}", socketPath);
        }
    }
}