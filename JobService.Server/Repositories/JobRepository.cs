using JobService.Data;
using JobService.Models;
using Microsoft.EntityFrameworkCore;

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
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
        {
            return false;
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Jobs.AnyAsync(j => j.Id == id);
    }
}