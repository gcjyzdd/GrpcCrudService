using JobService;
using JobService.Client.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobService.Client.Services;

public class JobServiceClient : IJobServiceClient
{
    private readonly global::JobService.JobService.JobServiceClient _grpcClient;
    private readonly ILogger<JobServiceClient> _logger;

    public JobServiceClient(global::JobService.JobService.JobServiceClient grpcClient, ILogger<JobServiceClient> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request)
    {
        try
        {
            _logger.LogInformation("Creating job: {JobName} in cluster: {ClusterName}", request.Name, request.ClusterName);
            var response = await _grpcClient.CreateJobAsync(request);
            _logger.LogInformation("Job creation {Status}: {Message}", response.Success ? "succeeded" : "failed", response.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job: {JobName}", request.Name);
            throw;
        }
    }

    public async Task<JobResponse> GetJobAsync(GetJobRequest request)
    {
        try
        {
            _logger.LogInformation("Getting job with ID: {JobId}", request.Id);
            var response = await _grpcClient.GetJobAsync(request);
            _logger.LogInformation("Job retrieval {Status}: {Message}", response.Success ? "succeeded" : "failed", response.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job with ID: {JobId}", request.Id);
            throw;
        }
    }

    public async Task<GetAllJobsResponse> GetAllJobsAsync(GetAllJobsRequest request)
    {
        try
        {
            _logger.LogInformation("Getting all jobs");
            var response = await _grpcClient.GetAllJobsAsync(request);
            _logger.LogInformation("Retrieved {JobCount} jobs", response.Jobs.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all jobs");
            throw;
        }
    }

    public async Task<JobResponse> UpdateJobAsync(UpdateJobRequest request)
    {
        try
        {
            _logger.LogInformation("Updating job with ID: {JobId}", request.Id);
            var response = await _grpcClient.UpdateJobAsync(request);
            _logger.LogInformation("Job update {Status}: {Message}", response.Success ? "succeeded" : "failed", response.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job with ID: {JobId}", request.Id);
            throw;
        }
    }

    public async Task<DeleteJobResponse> DeleteJobAsync(DeleteJobRequest request)
    {
        try
        {
            _logger.LogInformation("Deleting job with ID: {JobId}", request.Id);
            var response = await _grpcClient.DeleteJobAsync(request);
            _logger.LogInformation("Job deletion {Status}: {Message}", response.Success ? "succeeded" : "failed", response.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job with ID: {JobId}", request.Id);
            throw;
        }
    }
}