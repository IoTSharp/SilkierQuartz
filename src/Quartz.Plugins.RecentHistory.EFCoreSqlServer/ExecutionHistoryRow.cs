namespace Quartz.Plugins.RecentHistory.EFCoreSqlServer
{
public partial class ExecutionHistoryDbContext
    {
        internal sealed class ExecutionHistoryRow
        {
            public long Id { get; set; }

            public string FireInstanceId { get; set; }

            public string SchedulerInstanceId { get; set; }

            public string SchedulerName { get; set; }

            public string JobName { get; set; }

            public string TriggerName { get; set; }

            public string ScheduledFireTimeUtc { get; set; }

            public string ActualFireTimeUtc { get; set; }

            public bool Recovering { get; set; }

            public bool Vetoed { get; set; }

            public string FinishedTimeUtc { get; set; }

            public string ExceptionMessage { get; set; }
        }
    }
}
