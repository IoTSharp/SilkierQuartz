namespace Quartz.Plugins.RecentHistory.EFCoreSqlite
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
