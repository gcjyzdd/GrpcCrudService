using JobService.Models;

namespace JobService.Services;

public interface IJobTaskManager
{
    Task StartJobTaskAsync(int jobId, CancellationToken cancellationToken = default);
    Task<bool> CancelJobTaskAsync(int jobId);
    bool IsTaskRunning(int jobId);
    JobTaskStatus? GetTaskStatus(int jobId);
    Task<bool> WaitForTaskCancellation(int jobId, TimeSpan timeout);
}