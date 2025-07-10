using JobService;
using JobService.Client.Interfaces;
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
            // Test 1: Create a job
            _logger.LogInformation("1. Creating a job...");
            var createResponse = await _jobServiceClient.CreateJobAsync(new CreateJobRequest
            {
                Name = "Test Job",
                WorkDir = "/tmp/test",
                ClusterName = "test-cluster"
            });
            
            _logger.LogInformation("   Success: {Success}", createResponse.Success);
            _logger.LogInformation("   Message: {Message}", createResponse.Message);
            
            if (createResponse.Success)
            {
                _logger.LogInformation("   Created Job ID: {JobId}", createResponse.Job.Id);
                _logger.LogInformation("   Job Name: {JobName}", createResponse.Job.Name);
                _logger.LogInformation("   Work Dir: {WorkDir}", createResponse.Job.WorkDir);
                _logger.LogInformation("   Cluster: {ClusterName}", createResponse.Job.ClusterName);
            }

            // Test 2: Get all jobs
            _logger.LogInformation("2. Getting all jobs...");
            var getAllResponse = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());
            _logger.LogInformation("   Success: {Success}", getAllResponse.Success);
            _logger.LogInformation("   Message: {Message}", getAllResponse.Message);
            _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse.Jobs.Count);

            foreach (var job in getAllResponse.Jobs)
            {
                _logger.LogInformation("   - Job {JobId}: {JobName} ({ClusterName})", job.Id, job.Name, job.ClusterName);
            }

            if (getAllResponse.Jobs.Count > 0)
            {
                var firstJob = getAllResponse.Jobs[0];

                // Test 3: Get specific job
                _logger.LogInformation("3. Getting specific job...");
                var getResponse = await _jobServiceClient.GetJobAsync(new GetJobRequest { Id = firstJob.Id });
                _logger.LogInformation("   Success: {Success}", getResponse.Success);
                _logger.LogInformation("   Message: {Message}", getResponse.Message);
                
                if (getResponse.Success)
                {
                    _logger.LogInformation("   Job ID: {JobId}", getResponse.Job.Id);
                    _logger.LogInformation("   Job Name: {JobName}", getResponse.Job.Name);
                    _logger.LogInformation("   Work Dir: {WorkDir}", getResponse.Job.WorkDir);
                    _logger.LogInformation("   Cluster: {ClusterName}", getResponse.Job.ClusterName);
                    _logger.LogInformation("   Created: {CreatedAt}", getResponse.Job.CreatedAt);
                }

                // Test 4: Update job
                _logger.LogInformation("4. Updating job...");
                var updateResponse = await _jobServiceClient.UpdateJobAsync(new UpdateJobRequest
                {
                    Id = firstJob.Id,
                    Name = "Updated Test Job",
                    WorkDir = "/tmp/updated",
                    ClusterName = "updated-cluster"
                });
                _logger.LogInformation("   Success: {Success}", updateResponse.Success);
                _logger.LogInformation("   Message: {Message}", updateResponse.Message);
                
                if (updateResponse.Success)
                {
                    _logger.LogInformation("   Updated Job Name: {JobName}", updateResponse.Job.Name);
                    _logger.LogInformation("   Updated Work Dir: {WorkDir}", updateResponse.Job.WorkDir);
                    _logger.LogInformation("   Updated Cluster: {ClusterName}", updateResponse.Job.ClusterName);
                }

                // Test 5: Create another job for demonstration
                _logger.LogInformation("5. Creating another job...");
                var createResponse2 = await _jobServiceClient.CreateJobAsync(new CreateJobRequest
                {
                    Name = "Second Job",
                    WorkDir = "/tmp/second",
                    ClusterName = "second-cluster"
                });
                _logger.LogInformation("   Success: {Success}", createResponse2.Success);
                _logger.LogInformation("   Message: {Message}", createResponse2.Message);

                // Test 6: Get all jobs again to see both
                _logger.LogInformation("6. Getting all jobs again...");
                var getAllResponse2 = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());
                _logger.LogInformation("   Success: {Success}", getAllResponse2.Success);
                _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse2.Jobs.Count);

                foreach (var job in getAllResponse2.Jobs)
                {
                    _logger.LogInformation("   - Job {JobId}: {JobName} ({ClusterName})", job.Id, job.Name, job.ClusterName);
                }

                // Test 7: Delete first job
                _logger.LogInformation("7. Deleting first job...");
                var deleteResponse = await _jobServiceClient.DeleteJobAsync(new DeleteJobRequest { Id = firstJob.Id });
                _logger.LogInformation("   Success: {Success}", deleteResponse.Success);
                _logger.LogInformation("   Message: {Message}", deleteResponse.Message);

                // Test 8: Final get all jobs to confirm deletion
                _logger.LogInformation("8. Final check - getting all jobs...");
                var getAllResponse3 = await _jobServiceClient.GetAllJobsAsync(new GetAllJobsRequest());
                _logger.LogInformation("   Success: {Success}", getAllResponse3.Success);
                _logger.LogInformation("   Total jobs: {JobCount}", getAllResponse3.Jobs.Count);

                foreach (var job in getAllResponse3.Jobs)
                {
                    _logger.LogInformation("   - Job {JobId}: {JobName} ({ClusterName})", job.Id, job.Name, job.ClusterName);
                }
            }

            _logger.LogInformation("✅ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during testing");
            if (OperatingSystem.IsWindows())
            {
                _logger.LogError("Make sure the JobService server is running with named pipe: JobServicePipe");
            }
            else
            {
                var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
                _logger.LogError("Make sure the JobService server is running with Unix socket: {SocketPath}", socketPath);
            }
            throw;
        }
    }
}