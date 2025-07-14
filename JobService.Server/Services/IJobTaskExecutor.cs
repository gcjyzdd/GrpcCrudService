using JobService.Models;

namespace JobService.Services;

public interface IJobTaskExecutor
{
    Task ExecuteJobAsync(Models.Job job, CancellationToken cancellationToken);
}