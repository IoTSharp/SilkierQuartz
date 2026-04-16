using FakeItEasy;
using Quartz;
using Quartz.Plugins.RecentHistory;
using Quartz.Plugins.RecentHistory.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using SilkierQuartz.HostedService;
namespace SilkierQuartz.Test
{
    public class QuartzHostedServiceUnitTest
    {
        [Fact(DisplayName = "Install JobFactory (For DI)")]
        public async Task IServiceCollectionExtensions_Register_HostedService()
        {
            var serviceProvider = A.Fake<IServiceProvider>();
            A.CallTo(() => serviceProvider.GetService(typeof(IEnumerable<IScheduleJob>))).Returns(null);
            A.CallTo(() => serviceProvider.GetService(typeof(IExecutionHistoryStore))).Returns(null);

            var schedulerFactory = A.Fake<ISchedulerFactory>();
            var scheduler = A.Fake<IScheduler>();
            A.CallTo(() => schedulerFactory.GetScheduler(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(scheduler));

            var jobFactory = A.Fake<IJobFactory>();

            var testClass = new QuartzHostedService(serviceProvider, schedulerFactory, jobFactory);
            await testClass.StartAsync(CancellationToken.None);

            A.CallTo(scheduler)
                .Where(a => a.Method.Name.Equals("set_JobFactory"));
        }


        [Fact(DisplayName = "All Registered Job")]
        public async Task IServiceCollectionExtensions_Register_RegisterJob()
        {
            // Registered jobs
            var scheduleJobc = new List<IScheduleJob>();
            var jobDetail1 = A.Fake<IJobDetail>();
            var jobDetail2 = A.Fake<IJobDetail>();
            var jobDetail3 = A.Fake<IJobDetail>();

            scheduleJobc.Add(new ScheduleJob(jobDetail1, new List<ITrigger>() { A.Fake<ITrigger>() }));
            scheduleJobc.Add(new ScheduleJob(jobDetail2, new List<ITrigger>() { A.Fake<ITrigger>() }));
            scheduleJobc.Add(new ScheduleJob(jobDetail3, new List<ITrigger>() { A.Fake<ITrigger>() }));

            var serviceProvider = A.Fake<IServiceProvider>();
            A.CallTo(() => serviceProvider.GetService(typeof(IEnumerable<IScheduleJob>))).Returns(scheduleJobc);
            A.CallTo(() => serviceProvider.GetService(typeof(IExecutionHistoryStore))).Returns(null);


            var schedulerFactory = A.Fake<ISchedulerFactory>();
            var scheduler = A.Fake<IScheduler>();
            A.CallTo(() => schedulerFactory.GetScheduler(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(scheduler));

            var jobFactory = A.Fake<IJobFactory>();

            var testClass = new QuartzHostedService(serviceProvider, schedulerFactory, jobFactory);
            await testClass.StartAsync(CancellationToken.None);

            A.CallTo(
                () => scheduler.ScheduleJob(
                    A<IJobDetail>.That.Matches(jd => jd == jobDetail1 || jd == jobDetail2 || jd == jobDetail3),
                    A<ITrigger>.Ignored,
                    A<CancellationToken>.Ignored))
                .MustHaveHappened(3, Times.Exactly);
        }

        [Fact(DisplayName = "Started all registered Job with specified ITrigger")]
        public async Task IServiceCollectionExtensions_Register_RegisterJob_ITrigger()
        {
            // Registered jobas
            var scheduleJobc = new List<IScheduleJob>();
            var jobDetail1 = A.Fake<IJobDetail>();

            var trigger1 = A.Fake<ITrigger>();
            var trigger2 = A.Fake<ITrigger>();
            var trigger3 = A.Fake<ITrigger>();
            scheduleJobc.Add(new ScheduleJob(jobDetail1, new List<ITrigger>() { trigger1, trigger2, trigger3 }));

            var serviceProvider = A.Fake<IServiceProvider>();
            A.CallTo(() => serviceProvider.GetService(typeof(IEnumerable<IScheduleJob>))).Returns(scheduleJobc);
            A.CallTo(() => serviceProvider.GetService(typeof(IExecutionHistoryStore))).Returns(null);


            var schedulerFactory = A.Fake<ISchedulerFactory>();
            var scheduler = A.Fake<IScheduler>();
            A.CallTo(() => schedulerFactory.GetScheduler(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(scheduler));

            var jobFactory = A.Fake<IJobFactory>();

            var testClass = new QuartzHostedService(serviceProvider, schedulerFactory, jobFactory);
            await testClass.StartAsync(CancellationToken.None);

            A.CallTo(
                () => scheduler.ScheduleJob(
                    A<IJobDetail>.That.Matches(jd => jd == jobDetail1),
                    A<ITrigger>.That.Matches(t => t == trigger1),
                    A<CancellationToken>.Ignored))
                .MustHaveHappened(1,Times.Exactly);

            A.CallTo(
                () => scheduler.ScheduleJob(
                    A<ITrigger>.That.Matches(t => t == trigger2 || t == trigger3),
                    A<CancellationToken>.Ignored))
                .MustHaveHappened(2,Times.Exactly);
        }

        [Fact(DisplayName = "Registers execution history store in scheduler context before start")]
        public async Task IServiceCollectionExtensions_Register_ExecutionHistoryStore_InSchedulerContext()
        {
            var serviceProvider = A.Fake<IServiceProvider>();
            A.CallTo(() => serviceProvider.GetService(typeof(IEnumerable<IScheduleJob>))).Returns(null);

            var store = new InProcExecutionHistoryStore();
            A.CallTo(() => serviceProvider.GetService(typeof(IExecutionHistoryStore))).Returns(store);

            var schedulerFactory = A.Fake<ISchedulerFactory>();
            var scheduler = A.Fake<IScheduler>();
            var context = new SchedulerContext();
            A.CallTo(() => scheduler.Context).Returns(context);
            A.CallTo(() => schedulerFactory.GetScheduler(A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(scheduler));

            var jobFactory = A.Fake<IJobFactory>();

            var testClass = new QuartzHostedService(serviceProvider, schedulerFactory, jobFactory);
            await testClass.StartAsync(CancellationToken.None);

            context.GetExecutionHistoryStore().Should().BeSameAs(store);
        }
    }
}
