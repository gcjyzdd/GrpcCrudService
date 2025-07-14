using Microsoft.EntityFrameworkCore;
using JobService.Models;
using Microsoft.Data.Sqlite;

namespace JobService.Data
{
    public class JobContext : DbContext
    {
        public JobContext(DbContextOptions<JobContext> options) : base(options)
        {
        }

        public DbSet<Models.Job> Jobs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                // Set SQLite busy timeout to handle concurrent access
                optionsBuilder.UseSqlite(options =>
                {
                    options.CommandTimeout(30);
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.Job>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.WorkDir).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ClusterName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.TaskStatus).IsRequired();
                entity.Property(e => e.TaskStartedAt);
                entity.Property(e => e.TaskEndedAt);
                entity.Property(e => e.TaskErrorMessage).HasMaxLength(1000);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Set SQLite busy timeout before any database operation
            if (Database.GetDbConnection() is SqliteConnection connection)
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA busy_timeout = 30000";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}