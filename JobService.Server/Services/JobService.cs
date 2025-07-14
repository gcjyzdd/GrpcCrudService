using Grpc.Core;
using JobService.Models;
using JobService.Repositories;
using Google.Protobuf.WellKnownTypes;

namespace JobService.Services
{
    public class JobGrpcService : JobService.JobServiceBase
    {
        private readonly IJobRepository _jobRepository;
        private readonly IJobTaskManager _taskManager;
        private readonly ILogger<JobGrpcService> _logger;

        public JobGrpcService(
            IJobRepository jobRepository, 
            IJobTaskManager taskManager,
            ILogger<JobGrpcService> logger)
        {
            _jobRepository = jobRepository;
            _taskManager = taskManager;
            _logger = logger;
        }

        public override async Task<JobResponse> CreateJob(CreateJobRequest request, ServerCallContext context)
        {
            try
            {
                // Validate input data
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Job name cannot be empty"));
                }
                if (string.IsNullOrWhiteSpace(request.WorkDir))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Work directory cannot be empty"));
                }
                if (string.IsNullOrWhiteSpace(request.ClusterName))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Cluster name cannot be empty"));
                }

                var job = new Models.Job
                {
                    Name = request.Name,
                    WorkDir = request.WorkDir,
                    ClusterName = request.ClusterName,
                    CreatedAt = DateTime.UtcNow
                };

                var createdJob = await _jobRepository.CreateAsync(job);

                try
                {
                    await _taskManager.StartJobTaskAsync(createdJob.Id);
                    _logger.LogInformation("Started task for job {JobId}", createdJob.Id);
                }
                catch (Exception taskEx)
                {
                    _logger.LogError(taskEx, "Failed to start task for job {JobId}", createdJob.Id);
                    await _jobRepository.UpdateTaskStatusAsync(createdJob.Id, JobTaskStatus.Failed, taskEx.Message);
                }

                return new JobResponse
                {
                    Job = MapToProtoJob(createdJob),
                    Success = true,
                    Message = "Job created successfully"
                };
            }
            catch (RpcException)
            {
                throw; // Re-throw RpcExceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                throw new RpcException(new Status(StatusCode.Internal, $"Error creating job: {ex.Message}"));
            }
        }

        public override async Task<JobResponse> GetJob(GetJobRequest request, ServerCallContext context)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(request.Id);
                if (job == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));
                }

                return new JobResponse
                {
                    Job = MapToProtoJob(job),
                    Success = true,
                    Message = "Job retrieved successfully"
                };
            }
            catch (RpcException)
            {
                throw; // Re-throw RpcExceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job");
                throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving job: {ex.Message}"));
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
                // Validate input data
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Job name cannot be empty"));
                }
                if (string.IsNullOrWhiteSpace(request.WorkDir))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Work directory cannot be empty"));
                }
                if (string.IsNullOrWhiteSpace(request.ClusterName))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Cluster name cannot be empty"));
                }

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
                    throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));
                }

                return new JobResponse
                {
                    Job = MapToProtoJob(updatedJob),
                    Success = true,
                    Message = "Job updated successfully"
                };
            }
            catch (RpcException)
            {
                throw; // Re-throw RpcExceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job");
                throw new RpcException(new Status(StatusCode.Internal, $"Error updating job: {ex.Message}"));
            }
        }

        public override async Task<DeleteJobResponse> DeleteJob(DeleteJobRequest request, ServerCallContext context)
        {
            try
            {
                var job = await _jobRepository.GetJobWithStatusAsync(request.Id);
                if (job == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));
                }

                _logger.LogInformation("Starting deletion process for job {JobId}", request.Id);

                // Step 1: Mark job for deletion (without transaction to avoid long locks)
                if (job.TaskStatus == JobTaskStatus.Running)
                {
                    await _jobRepository.UpdateTaskStatusAsync(request.Id, JobTaskStatus.Cancelling);
                    
                    // Step 2: Cancel the task (this can take time, but no DB transaction is held)
                    var cancellationSuccess = await _taskManager.CancelJobTaskAsync(request.Id);
                    if (!cancellationSuccess)
                    {
                        _logger.LogWarning("Task cancellation timed out for job {JobId}", request.Id);
                    }

                    // Step 3: Wait for task to finish (with shorter timeout)
                    var waitSuccess = await _taskManager.WaitForTaskCancellation(request.Id, TimeSpan.FromSeconds(10));
                    if (!waitSuccess)
                    {
                        // If task doesn't finish quickly, return error and let user retry
                        await _jobRepository.UpdateTaskStatusAsync(request.Id, JobTaskStatus.Running);
                        throw new RpcException(new Status(StatusCode.DeadlineExceeded, 
                            "Task cancellation in progress. Please try again in a few seconds."));
                    }
                }

                // Step 4: Quick transaction for actual deletion
                using var transaction = await _jobRepository.BeginTransactionAsync();
                try
                {
                    // Double-check job still exists and is in cancelling state
                    var currentJob = await _jobRepository.GetJobWithStatusAsync(request.Id);
                    if (currentJob == null)
                    {
                        throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));
                    }

                    if (currentJob.TaskStatus == JobTaskStatus.Running)
                    {
                        await transaction.RollbackAsync();
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, 
                            "Job task is still running. Please cancel the task first."));
                    }

                    var deleted = await _jobRepository.DeleteAsync(request.Id);
                    if (!deleted)
                    {
                        await transaction.RollbackAsync();
                        throw new RpcException(new Status(StatusCode.Internal, "Failed to delete job from database"));
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Successfully deleted job {JobId}", request.Id);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return new DeleteJobResponse
                {
                    Success = true,
                    Message = "Job deleted successfully"
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, $"Error deleting job: {ex.Message}"));
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