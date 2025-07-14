using System.Collections.Concurrent;
using JobService.Models;
using JobService.Repositories;

namespace JobService.Services;

public class TaskContext
{
    public TaskContext(CancellationTokenSource cancellationTokenSource)
    {
        CancellationTokenSource = cancellationTokenSource;
    }

    public CancellationTokenSource CancellationTokenSource { get; }
    public Task Task { get; set; } = Task.CompletedTask;
}

public class JobTaskManager : IJobTaskManager
{
    private readonly ConcurrentDictionary<int, TaskContext> _runningTasks = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobTaskManager> _logger;

    public JobTaskManager(
        IServiceProvider serviceProvider,
        ILogger<JobTaskManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartJobTaskAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var context = new TaskContext(cts);

        if (!_runningTasks.TryAdd(jobId, context))
        {
            cts.Dispose();
            throw new InvalidOperationException($"Task for job {jobId} is already running");
        }

        _logger.LogInformation("Starting task for job {JobId}", jobId);

        using (var scope = _serviceProvider.CreateScope())
        {
            var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            await jobRepository.UpdateTaskStatusAsync(jobId, JobTaskStatus.Running);
        }

        context.Task = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                var jobTaskExecutor = scope.ServiceProvider.GetRequiredService<IJobTaskExecutor>();

                var job = await jobRepository.GetByIdAsync(jobId);
                if (job != null)
                {
                    _logger.LogInformation("Executing task for job {JobId}", jobId);
                    await jobTaskExecutor.ExecuteJobAsync(job, cts.Token);
                    await jobRepository.UpdateTaskStatusAsync(jobId, JobTaskStatus.Completed);
                    _logger.LogInformation("Task completed for job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Job {JobId} not found when executing task", jobId);
                    await jobRepository.UpdateTaskStatusAsync(jobId, JobTaskStatus.Failed, "Job not found");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task cancelled for job {JobId}", jobId);
                using var scope = _serviceProvider.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                await jobRepository.UpdateTaskStatusAsync(jobId, JobTaskStatus.Cancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task failed for job {JobId}", jobId);
                using var scope = _serviceProvider.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                await jobRepository.UpdateTaskStatusAsync(jobId, JobTaskStatus.Failed, ex.Message);
            }
            finally
            {
                _runningTasks.TryRemove(jobId, out _);
                cts.Dispose();
            }
        }, cts.Token);
    }

    public async Task<bool> CancelJobTaskAsync(int jobId)
    {
        if (_runningTasks.TryGetValue(jobId, out var context))
        {
            _logger.LogInformation("Cancelling task for job {JobId}", jobId);
            
            // Don't update database status here - let the task itself update it when it finishes
            // This reduces database contention during cancellation
            context.CancellationTokenSource.Cancel();

            try
            {
                await context.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return true;
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Task cancellation timed out for job {JobId}", jobId);
                return false;
            }
        }

        _logger.LogInformation("No running task found for job {JobId}, considering it cancelled", jobId);
        return true;
    }

    public bool IsTaskRunning(int jobId)
    {
        return _runningTasks.ContainsKey(jobId);
    }

    public JobTaskStatus? GetTaskStatus(int jobId)
    {
        if (_runningTasks.TryGetValue(jobId, out var context))
        {
            if (context.Task.IsCompleted)
            {
                return context.Task.IsCanceled ? JobTaskStatus.Cancelled : 
                       context.Task.IsFaulted ? JobTaskStatus.Failed : JobTaskStatus.Completed;
            }
            return JobTaskStatus.Running;
        }
        return null;
    }

    public async Task<bool> WaitForTaskCancellation(int jobId, TimeSpan timeout)
    {
        if (_runningTasks.TryGetValue(jobId, out var context))
        {
            try
            {
                await context.Task.WaitAsync(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
        return true;
    }
}