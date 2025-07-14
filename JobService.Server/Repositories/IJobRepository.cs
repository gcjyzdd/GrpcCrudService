using JobService.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace JobService.Repositories;

public interface IJobRepository
{
    Task<Models.Job> CreateAsync(Models.Job job);
    Task<Models.Job?> GetByIdAsync(int id);
    Task<IEnumerable<Models.Job>> GetAllAsync();
    Task<Models.Job?> UpdateAsync(Models.Job job);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    
    Task<bool> UpdateTaskStatusAsync(int jobId, JobTaskStatus status, string? errorMessage = null);
    Task<Models.Job?> GetJobWithStatusAsync(int id);
    Task<IDbContextTransaction> BeginTransactionAsync();
}