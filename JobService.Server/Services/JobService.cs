using Grpc.Core;
using JobService.Models;
using JobService.Repositories;
using Google.Protobuf.WellKnownTypes;

namespace JobService.Services
{
    public class JobGrpcService : JobService.JobServiceBase
    {
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<JobGrpcService> _logger;

        public JobGrpcService(IJobRepository jobRepository, ILogger<JobGrpcService> logger)
        {
            _jobRepository = jobRepository;
            _logger = logger;
        }

        public override async Task<JobResponse> CreateJob(CreateJobRequest request, ServerCallContext context)
        {
            try
            {
                var job = new Models.Job
                {
                    Name = request.Name,
                    WorkDir = request.WorkDir,
                    ClusterName = request.ClusterName,
                    CreatedAt = DateTime.UtcNow
                };

                var createdJob = await _jobRepository.CreateAsync(job);

                return new JobResponse
                {
                    Job = MapToProtoJob(createdJob),
                    Success = true,
                    Message = "Job created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                return new JobResponse
                {
                    Success = false,
                    Message = $"Error creating job: {ex.Message}"
                };
            }
        }

        public override async Task<JobResponse> GetJob(GetJobRequest request, ServerCallContext context)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(request.Id);
                if (job == null)
                {
                    return new JobResponse
                    {
                        Success = false,
                        Message = "Job not found"
                    };
                }

                return new JobResponse
                {
                    Job = MapToProtoJob(job),
                    Success = true,
                    Message = "Job retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job");
                return new JobResponse
                {
                    Success = false,
                    Message = $"Error retrieving job: {ex.Message}"
                };
            }
        }

        public override async Task<GetAllJobsResponse> GetAllJobs(GetAllJobsRequest request, ServerCallContext context)
        {
            try
            {
                var jobs = await _jobRepository.GetAllAsync();
                var response = new GetAllJobsResponse
                {
                    Success = true,
                    Message = "Jobs retrieved successfully"
                };

                foreach (var job in jobs)
                {
                    response.Jobs.Add(MapToProtoJob(job));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs");
                return new GetAllJobsResponse
                {
                    Success = false,
                    Message = $"Error retrieving jobs: {ex.Message}"
                };
            }
        }

        public override async Task<JobResponse> UpdateJob(UpdateJobRequest request, ServerCallContext context)
        {
            try
            {
                var job = new Models.Job
                {
                    Id = request.Id,
                    Name = request.Name,
                    WorkDir = request.WorkDir,
                    ClusterName = request.ClusterName
                };

                var updatedJob = await _jobRepository.UpdateAsync(job);
                if (updatedJob == null)
                {
                    return new JobResponse
                    {
                        Success = false,
                        Message = "Job not found"
                    };
                }

                return new JobResponse
                {
                    Job = MapToProtoJob(updatedJob),
                    Success = true,
                    Message = "Job updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job");
                return new JobResponse
                {
                    Success = false,
                    Message = $"Error updating job: {ex.Message}"
                };
            }
        }

        public override async Task<DeleteJobResponse> DeleteJob(DeleteJobRequest request, ServerCallContext context)
        {
            try
            {
                var deleted = await _jobRepository.DeleteAsync(request.Id);
                if (!deleted)
                {
                    return new DeleteJobResponse
                    {
                        Success = false,
                        Message = "Job not found"
                    };
                }

                return new DeleteJobResponse
                {
                    Success = true,
                    Message = "Job deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job");
                return new DeleteJobResponse
                {
                    Success = false,
                    Message = $"Error deleting job: {ex.Message}"
                };
            }
        }

        private static Job MapToProtoJob(Models.Job job)
        {
            return new Job
            {
                Id = job.Id,
                Name = job.Name,
                WorkDir = job.WorkDir,
                ClusterName = job.ClusterName,
                CreatedAt = Timestamp.FromDateTime(job.CreatedAt.ToUniversalTime())
            };
        }
    }
}