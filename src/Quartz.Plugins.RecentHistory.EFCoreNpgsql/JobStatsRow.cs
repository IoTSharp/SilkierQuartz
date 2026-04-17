namespace Quartz.Plugins.RecentHistory.EFCoreNpgsql
{
public partial class ExecutionHistoryDbContext
    {
        internal sealed class JobStatsRow
        {
            public int Id { get; set; }

            public int TotalJobsExecuted { get; set; }

            public int TotalJobsFailed { get; set; }
        }
    }
}
