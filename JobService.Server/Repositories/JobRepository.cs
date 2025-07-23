using JobService.Data;
using JobService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JobService.Repositories;

public class JobRepository : IJobRepository
{
    private readonly JobContext _context;

    public JobRepository(JobContext context)
    {
        _context = context;
    }

    public async Task<Models.Job> CreateAsync(Models.Job job)
    {
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<Models.Job?> GetByIdAsync(int id)
    {
        return await _context.Jobs.FindAsync(id);
    }

    public async Task<IEnumerable<Models.Job>> GetAllAsync()
    {
        return await _context.Jobs.ToListAsync();
    }

    public async Task<Models.Job?> UpdateAsync(Models.Job job)
    {
        var existingJob = await _context.Jobs.FindAsync(job.Id);
        if (existingJob == null)
        {
            return null;
        }

        existingJob.Name = job.Name;
        existingJob.WorkDir = job.WorkDir;
        existingJob.ClusterName = job.ClusterName;

        await _context.SaveChangesAsync();
        return existingJob;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return false;
                }

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
            {
                if (attempt == maxRetries - 1) throw;
                
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }

        return false;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Jobs.AnyAsync(j => j.Id == id);
    }

    public async Task<bool> UpdateTaskStatusAsync(int jobId, JobTaskStatus status, string? errorMessage = null)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return false;
                }

                job.TaskStatus = status;
                
                switch (status)
                {
                    case JobTaskStatus.Running:
                        job.TaskStartedAt = DateTime.UtcNow;
                        job.TaskErrorMessage = null;
                        break;
                    case JobTaskStatus.Completed:
                    case JobTaskStatus.Cancelled:
                    case JobTaskStatus.Failed:
                        job.TaskEndedAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            job.TaskErrorMessage = errorMessage;
                        }
                        break;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
            {
                if (attempt == maxRetries - 1) throw;
                
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }

        return false;
    }

    public async Task<bool> UpdateProgressAsync(int jobId, float progress)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return false;
                }

                job.Progress = Math.Clamp(progress, 0.0f, 100.0f);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
            {
                if (attempt == maxRetries - 1) throw;
                
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }

        return false;
    }

    public async Task<Models.Job?> GetJobWithStatusAsync(int id)
    {
        return await _context.Jobs.FindAsync(id);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }
}