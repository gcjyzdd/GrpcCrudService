using System.ComponentModel.DataAnnotations;

namespace JobService.Models
{
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
    }
}