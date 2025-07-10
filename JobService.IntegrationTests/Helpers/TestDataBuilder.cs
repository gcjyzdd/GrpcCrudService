using JobService;
using JobService.Common;

namespace JobService.IntegrationTests.Helpers;

public static class TestDataBuilder
{
    public static CreateJobRequest CreateValidJobRequest(string? name = null, string? workdir = null, string? clusterName = null)
    {
        return new CreateJobRequest
        {
            Name = name ?? $"TestJob_{Guid.NewGuid().ToString("N")[..8]}",
            WorkDir = workdir ?? "/tmp/test",
            ClusterName = clusterName ?? "test-cluster"
        };
    }

    public static UpdateJobRequest CreateUpdateJobRequest(int id, string? name = null, string? workdir = null, string? clusterName = null)
    {
        return new UpdateJobRequest
        {
            Id = id,
            Name = name ?? $"UpdatedJob_{Guid.NewGuid().ToString("N")[..8]}",
            WorkDir = workdir ?? "/tmp/updated",
            ClusterName = clusterName ?? "updated-cluster"
        };
    }

    public static GetJobRequest CreateGetJobRequest(int id)
    {
        return new GetJobRequest { Id = id };
    }

    public static DeleteJobRequest CreateDeleteJobRequest(int id)
    {
        return new DeleteJobRequest { Id = id };
    }

    public static GetAllJobsRequest CreateGetAllJobsRequest()
    {
        return new GetAllJobsRequest();
    }
}