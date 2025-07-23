using System.ComponentModel.DataAnnotations;

namespace JobService.Models
{
    public enum JobTaskStatus
    {
        NotStarted,
        Running,
        Cancelling,
        Cancelled,
        Completed,
        Failed
    }

    public class Job
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string WorkDir { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string ClusterName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public float Progress { get; set; } = 0.0f;
        
        public JobTaskStatus TaskStatus { get; set; } = JobTaskStatus.NotStarted;
        
        public DateTime? TaskStartedAt { get; set; }
        
        public DateTime? TaskEndedAt { get; set; }
        
        [MaxLength(1000)]
        public string? TaskErrorMessage { get; set; }
    }
}