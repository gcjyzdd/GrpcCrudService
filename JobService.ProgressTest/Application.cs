using JobService;
using JobService.Grpc.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobService.ProgressTest;

public class Application
{
    private readonly IJobServiceClient _jobServiceClient;
    private readonly ILogger<Application> _logger;

    public Application(IJobServiceClient jobServiceClient, ILogger<Application> logger)
    {
        _jobServiceClient = jobServiceClient;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("JobService Progress Test - Rsync Progress Monitoring");
        _logger.LogInformation("=====================================================");

        try
        {
            // Get input folder from user
            string? inputFolder = null;
            while (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
            {
                Console.Write("Enter the path to the folder you want to copy: ");
                inputFolder = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(inputFolder))
                {
                    Console.WriteLine("❌ Please enter a valid folder path.");
                    continue;
                }

                if (!Directory.Exists(inputFolder))
                {
                    Console.WriteLine($"❌ Folder '{inputFolder}' does not exist. Please try again.");
                    inputFolder = null;
                }
            }

            _logger.LogInformation("Input folder: {InputFolder}", inputFolder);

            // Submit the job
            _logger.LogInformation("Submitting job to copy data...");
            var createResponse = await _jobServiceClient.CreateJobAsync(new CreateJobRequest
            {
                Name = $"Copy {Path.GetFileName(inputFolder)}",
                WorkDir = inputFolder,
                ClusterName = "progress-test-cluster"
            });

            if (!createResponse.Success)
            {
                _logger.LogError("❌ Failed to create job: {Message}", createResponse.Message);
                return;
            }

            var jobId = createResponse.Job.Id;
            _logger.LogInformation("✅ Job created successfully! Job ID: {JobId}", jobId);
            _logger.LogInformation("Initial Progress: {Progress}%", createResponse.Job.Progress);

            // Monitor progress
            await MonitorJobProgress(jobId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during progress test");
            LogConnectionTroubleshooting();
            throw;
        }
    }

    private async Task MonitorJobProgress(int jobId)
    {
        _logger.LogInformation("Starting progress monitoring (checking every 1 second)...");
        _logger.LogInformation("Press Ctrl+C to stop monitoring");

        var lastProgress = -1.0f;
        var startTime = DateTime.UtcNow;

        while (true)
        {
            try
            {
                var jobResponse = await _jobServiceClient.GetJobAsync(new GetJobRequest { Id = jobId });

                if (!jobResponse.Success)
                {
                    _logger.LogError("❌ Failed to get job status: {Message}", jobResponse.Message);
                    break;
                }

                var job = jobResponse.Job;
                var currentProgress = job.Progress;
                var elapsed = DateTime.UtcNow - startTime;

                // Only log when progress changes or every 10 seconds
                if (Math.Abs(currentProgress - lastProgress) > 0.1f || elapsed.TotalSeconds % 10 < 1)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Progress: {currentProgress:F1}% | Elapsed: {elapsed:mm\\:ss}");
                    lastProgress = currentProgress;
                }

                // Check if job is completed
                if (currentProgress >= 100.0f)
                {
                    _logger.LogInformation("🎉 Job completed successfully!");
                    _logger.LogInformation("Final Status:");
                    _logger.LogInformation("  - Job ID: {JobId}", job.Id);
                    _logger.LogInformation("  - Job Name: {JobName}", job.Name);
                    _logger.LogInformation("  - Work Dir: {WorkDir}", job.WorkDir);
                    _logger.LogInformation("  - Cluster: {ClusterName}", job.ClusterName);
                    _logger.LogInformation("  - Progress: {Progress}%", job.Progress);
                    _logger.LogInformation("  - Created: {CreatedAt}", job.CreatedAt);
                    _logger.LogInformation("  - Total Time: {TotalTime}", elapsed.ToString(@"mm\:ss"));

                    var destinationPath = $"/tmp/data/job_{jobId}/";
                    _logger.LogInformation("  - Destination: {DestinationPath}", destinationPath);

                    if (Directory.Exists(destinationPath))
                    {
                        var fileCount = Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories).Length;
                        _logger.LogInformation("  - Files copied: {FileCount}", fileCount);
                    }

                    break;
                }

                // Wait 1 second before next check
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while monitoring progress");
                break;
            }
        }
    }

    private void LogConnectionTroubleshooting()
    {
        if (OperatingSystem.IsWindows())
        {
            _logger.LogError("Make sure the JobService server is running with named pipe: JobServicePipe");
        }
        else
        {
            var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
            _logger.LogError("Make sure the JobService server is running with Unix socket: {SocketPath}", socketPath);
        }
    }
}