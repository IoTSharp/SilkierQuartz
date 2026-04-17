namespace Quartz.Plugins.RecentHistory.EFCoreSqlServer
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
