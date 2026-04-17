using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SilkierQuartz
{
    public static class DemoScheduler
    {
        /// <summary>
        /// How to compatible old code to SilkierQuartz
        /// </summary>
        /// <param name="app"></param>
        public static void SchedulerJobs(this IApplicationBuilder app)
        {
            var scheduler = app.GetScheduler();
            {
                var jobData = new JobDataMap();
                jobData.Put("DateFrom", DateTime.Now);
                jobData.Put("QuartzAssembly", File.ReadAllBytes(typeof(IScheduler).Assembly.Location));

                var job = JobBuilder.Create<DummyJob>()
                    .WithIdentity("Sales", "REPORTS")
                    .WithDescription("Hello Job!")
                    .UsingJobData(jobData)
                    .StoreDurably()
                    .Build();
                var trigger = TriggerBuilder.Create()
                    .WithIdentity("MorningSales")
                    .StartNow()
                    .WithCronSchedule("0 0 8 1/1 * ? *")
                    .Build();
                if (!scheduler.CheckExists(job.Key).GetAwaiter().GetResult())
                {
                    scheduler.ScheduleJob(job, trigger).Wait();
                }


                trigger = TriggerBuilder.Create()
                    .WithIdentity("MonthlySales")
                    .ForJob(job.Key)
                    .StartNow()
                    .WithCronSchedule("0 0 12 1 1/1 ? *")
                    .Build();
                if (!scheduler.CheckExists(trigger.Key).GetAwaiter().GetResult())
                {
                    scheduler.ScheduleJob(trigger).Wait(); ;
                }
                scheduler.PauseTrigger(trigger.Key).Wait(); ;

                trigger = TriggerBuilder.Create()
                    .WithIdentity("HourlySales")
                    .ForJob(job.Key)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
                    .Build();
                if (!scheduler.CheckExists(trigger.Key).GetAwaiter().GetResult())
                {
                    scheduler.ScheduleJob(trigger).Wait(); ;
                }
            }
            Task.Run(async () =>
                        {
                            var job = JobBuilder.Create<DummyJob>().WithIdentity("Job1").StoreDurably().Build();
                            await scheduler.AddJob(job, false);
                            job = JobBuilder.Create<DummyJob>().WithIdentity("Job2").StoreDurably().Build();
                            await scheduler.AddJob(job, false);
                            job = JobBuilder.Create<DummyJob>().WithIdentity("Job3").StoreDurably().Build();
                            await scheduler.AddJob(job, false);
                            job = JobBuilder.Create<DummyJob>().WithIdentity("Job4").StoreDurably().Build();
                            await scheduler.AddJob(job, false);
                            job = JobBuilder.Create<DummyJob>().WithIdentity("Job5").StoreDurably().Build();
                            await scheduler.AddJob(job, false);
                            job = JobBuilder.Create<DummyJob>().WithIdentity("Send SMS", "CRITICAL").StoreDurably().RequestRecovery().Build();
                            await scheduler.AddJob(job, false);

                            var trigger = TriggerBuilder.Create()
                                .WithIdentity("PushAds  (US)")
                                .ForJob(job.Key)
                                .UsingJobData("Location", "US")
                                .StartNow()
                                .WithCronSchedule("0 0/5 * 1/1 * ? *")
                                .Build();
                            await scheduler.ScheduleJob(trigger);

                            trigger = TriggerBuilder.Create()
                                .WithIdentity("PushAds (EU)")
                                .ForJob(job.Key)
                                .UsingJobData("Location", "EU")
                                .StartNow()
                                .WithCronSchedule("0 0/7 * 1/1 * ? *")
                                .Build();
                            await scheduler.ScheduleJob(trigger);
                            await scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals("LONGRUNNING"));

                            job = JobBuilder.Create<DummyJob>().WithIdentity("Send Push", "CRITICAL").StoreDurably().RequestRecovery().Build();
                            await scheduler.AddJob(job, false);
                        });
            Task.Run(async () =>
            {
                var job = JobBuilder.Create<DisallowConcurrentJob>()
                    .WithIdentity("Load CSV", "IMPORT")
                    .StoreDurably()
                    .Build();
                var trigger = TriggerBuilder.Create()
                    .WithIdentity("CSV_small", "FREQUENTLY")
                    .ForJob(job)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
                    .Build();
                await scheduler.ScheduleJob(job, trigger);
                trigger = TriggerBuilder.Create()
                    .WithIdentity("CSV_big", "LONGRUNNING")
                    .ForJob(job)
                    .StartNow()
                    .WithDailyTimeIntervalSchedule(x => x.OnMondayThroughFriday())
                    .Build();
                await scheduler.ScheduleJob(trigger);
            });


        }

        private class DummyJob : IJob
        {
            private static readonly Random Random = new Random();

            public async Task Execute(IJobExecutionContext context)
            {
                Debug.WriteLine("DummyJob > " + DateTime.Now);

                await Task.Delay(TimeSpan.FromSeconds(Random.Next(1, 20)));

                if (Random.Next(2) == 0)
                    throw new Exception("Fatal error example!");
            }
        }

        [DisallowConcurrentExecution, PersistJobDataAfterExecution]
        public class DisallowConcurrentJob : IJob
        {
            private static readonly Random Random = new Random();

            public async Task Execute(IJobExecutionContext context)
            {
                Debug.WriteLine("DisallowConcurrentJob > " + DateTime.Now);

                context.JobDetail.JobDataMap.Put("LastExecuted", DateTime.Now);

                await Task.Delay(TimeSpan.FromSeconds(Random.Next(1, 5)));

                if (Random.Next(4) == 0)
                    throw new Exception("Fatal error example!");
            }
        }
    }
}
