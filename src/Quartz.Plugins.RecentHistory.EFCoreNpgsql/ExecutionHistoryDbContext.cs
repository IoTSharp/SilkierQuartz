using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quartz.Plugins.RecentHistory;

namespace Quartz.Plugins.RecentHistory.EFCoreNpgsql
{
    /// <summary>
    /// EF Core database context for SilkierQuartz execution history data.
    /// </summary>
    public partial class ExecutionHistoryDbContext : DbContext
    {
        internal const string DefaultTablePrefix = ExecutionHistoryStoreOptions.DefaultTablePrefix;

        public ExecutionHistoryDbContext(DbContextOptions<ExecutionHistoryDbContext> options)
            : base(options)
        {
        }

        internal DbSet<ExecutionHistoryRow> ExecutionHistory => Set<ExecutionHistoryRow>();

        internal DbSet<JobStatsRow> JobStats => Set<JobStatsRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var boolConverter = new BoolToZeroOneConverter<int>();

            modelBuilder.Entity<ExecutionHistoryRow>(entity =>
            {
                entity.ToTable(DefaultTablePrefix + "ExecutionHistoryStore");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.FireInstanceId).IsUnique();
                entity.HasIndex(x => new { x.SchedulerName, x.JobName });
                entity.HasIndex(x => new { x.SchedulerName, x.TriggerName });

                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.FireInstanceId).HasColumnName("fire_instance_id").HasMaxLength(512).IsRequired();
                entity.Property(x => x.SchedulerInstanceId).HasColumnName("scheduler_instance_id").HasMaxLength(512).IsRequired();
                entity.Property(x => x.SchedulerName).HasColumnName("scheduler_name").HasMaxLength(512).IsRequired();
                entity.Property(x => x.JobName).HasColumnName("job_name").HasMaxLength(512).IsRequired();
                entity.Property(x => x.TriggerName).HasColumnName("trigger_name").HasMaxLength(512).IsRequired();
                entity.Property(x => x.ScheduledFireTimeUtc).HasColumnName("scheduled_fire_time_utc").HasMaxLength(64);
                entity.Property(x => x.ActualFireTimeUtc).HasColumnName("actual_fire_time_utc").HasMaxLength(64).IsRequired();
                entity.Property(x => x.Recovering).HasColumnName("recovering").HasConversion(boolConverter).IsRequired();
                entity.Property(x => x.Vetoed).HasColumnName("vetoed").HasConversion(boolConverter).IsRequired();
                entity.Property(x => x.FinishedTimeUtc).HasColumnName("finished_time_utc").HasMaxLength(64);
                entity.Property(x => x.ExceptionMessage).HasColumnName("exception_message");
            });

            modelBuilder.Entity<JobStatsRow>(entity =>
            {
                entity.ToTable(DefaultTablePrefix + "JobStats");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(x => x.TotalJobsExecuted).HasColumnName("total_jobs_executed").IsRequired();
                entity.Property(x => x.TotalJobsFailed).HasColumnName("total_jobs_failed").IsRequired();
            });
        }
    }
}
