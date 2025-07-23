using System.Diagnostics;
using System.Text.RegularExpressions;
using JobService.Models;
using JobService.Repositories;

namespace JobService.Services;

public class RsyncJobTaskExecutor : IJobTaskExecutor
{
    private readonly ILogger<RsyncJobTaskExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static readonly Regex ProgressRegex = new(@"(\d+)%", RegexOptions.Compiled);

    public RsyncJobTaskExecutor(ILogger<RsyncJobTaskExecutor> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteJobAsync(Models.Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting rsync for job {JobId} '{JobName}' from '{WorkDir}' to '/tmp/data/job_{JobId}/'", 
            job.Id, job.Name, job.WorkDir, job.Id);

        var destinationPath = $"/tmp/data/job_{job.Id}/";
        
        try
        {
            // Create destination directory
            Directory.CreateDirectory(destinationPath);
            
            // Prepare rsync command
            var startInfo = new ProcessStartInfo
            {
                FileName = "rsync",
                Arguments = $"--progress --recursive --human-readable \"{job.WorkDir}/\" \"{destinationPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            // Handle progress updates
            process.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    await HandleProgressOutput(job.Id, e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogWarning("Rsync stderr for job {JobId}: {Error}", job.Id, e.Data);
                }
            };

            _logger.LogDebug("Starting rsync process for job {JobId}: {Command} {Arguments}", 
                job.Id, startInfo.FileName, startInfo.Arguments);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with cancellation support
            while (!process.HasExited)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(500, cancellationToken);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Rsync failed with exit code {process.ExitCode}");
            }

            // Set final progress to 100%
            await UpdateProgress(job.Id, 100.0f);
            
            _logger.LogInformation("Completed rsync for job {JobId} '{JobName}'", job.Id, job.Name);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Rsync cancelled for job {JobId}", job.Id);
            
            // Clean up partial transfer
            try
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                    _logger.LogDebug("Cleaned up partial transfer directory for job {JobId}", job.Id);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up directory for cancelled job {JobId}", job.Id);
            }
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rsync failed for job {JobId}: {Error}", job.Id, ex.Message);
            throw;
        }
    }

    private async Task HandleProgressOutput(int jobId, string output)
    {
        try
        {
            // Parse rsync progress output
            // Example: "1,234,567  45%   12.34MB/s    0:01:23"
            var match = ProgressRegex.Match(output);
            if (match.Success && float.TryParse(match.Groups[1].Value, out var progress))
            {
                await UpdateProgress(jobId, progress);
                _logger.LogDebug("Job {JobId} progress: {Progress}%", jobId, progress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse progress for job {JobId} from output: {Output}", jobId, output);
        }
    }

    private async Task UpdateProgress(int jobId, float progress)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            await jobRepository.UpdateProgressAsync(jobId, progress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update progress for job {JobId}", jobId);
        }
    }
}