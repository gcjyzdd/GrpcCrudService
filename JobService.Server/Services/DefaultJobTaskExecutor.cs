using JobService.Models;

namespace JobService.Services;

public class DefaultJobTaskExecutor : IJobTaskExecutor
{
    private readonly ILogger<DefaultJobTaskExecutor> _logger;

    public DefaultJobTaskExecutor(ILogger<DefaultJobTaskExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteJobAsync(Models.Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting execution of job {JobId} '{JobName}' in cluster '{ClusterName}' with workdir '{WorkDir}'", 
            job.Id, job.Name, job.ClusterName, job.WorkDir);

        var duration = TimeSpan.FromMinutes(5);
        var checkInterval = TimeSpan.FromSeconds(10);
        var totalChecks = (int)(duration.TotalMilliseconds / checkInterval.TotalMilliseconds);

        for (int i = 0; i < totalChecks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Job {JobId} progress: {Progress}% ({Current}/{Total})", 
                job.Id, (i + 1) * 100 / totalChecks, i + 1, totalChecks);

            await Task.Delay(checkInterval, cancellationToken);
        }

        _logger.LogInformation("Completed execution of job {JobId} '{JobName}'", job.Id, job.Name);
    }
}