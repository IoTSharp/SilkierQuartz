using FakeItEasy;
using FluentAssertions;
using Quartz;
using Quartz.Plugins.RecentHistory;
using Quartz.Plugins.RecentHistory.Impl;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SilkierQuartz.Test
{
    public class ExecutionHistoryPluginUnitTest
    {
        [Fact(DisplayName = "Falls back to in-proc execution history store when no store is configured")]
        public async Task ExecutionHistoryPlugin_Uses_InProcStore_ByDefault()
        {
            var scheduler = A.Fake<IScheduler>();
            var context = new SchedulerContext();
            A.CallTo(() => scheduler.Context).Returns(context);
            A.CallTo(() => scheduler.SchedulerName).Returns("scheduler");
            A.CallTo(() => scheduler.ListenerManager).Returns(A.Fake<IListenerManager>());

            var plugin = new ExecutionHistoryPlugin();
            await plugin.Initialize("recentHistory", scheduler, CancellationToken.None);
            await plugin.Start(CancellationToken.None);

            context.GetExecutionHistoryStore().Should().BeOfType<InProcExecutionHistoryStore>();
        }
    }
}
