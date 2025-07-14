using JobService;

namespace JobService.Grpc.Interfaces;

public interface IJobServiceClient
{
    Task<JobResponse> CreateJobAsync(CreateJobRequest request);
    Task<JobResponse> GetJobAsync(GetJobRequest request);
    Task<GetAllJobsResponse> GetAllJobsAsync(GetAllJobsRequest request);
    Task<JobResponse> UpdateJobAsync(UpdateJobRequest request);
    Task<DeleteJobResponse> DeleteJobAsync(DeleteJobRequest request);
}