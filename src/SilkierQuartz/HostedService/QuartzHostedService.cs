using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Impl;
using Quartz;
using Quartz.Spi;
using System.Linq;

namespace SilkierQuartz.HostedService
{
    internal class QuartzHostedService : IHostedService
    {
        private IServiceProvider Services { get; }
        private IScheduler _scheduler;
        private ISchedulerFactory _schedulerFactory;
        private IJobFactory _jobFactory { get; }

        public QuartzHostedService(
            IServiceProvider services,
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory)
        {
            Services = services;
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var _scheduleJobs = Services.GetService<IEnumerable<IScheduleJob>>();

            _scheduler = await _schedulerFactory.GetScheduler();
            _scheduler.JobFactory = _jobFactory;

            await _scheduler.Start(cancellationToken);

            if (_scheduleJobs == null || !_scheduleJobs.Any())
                return;

            foreach (var scheduleJob in _scheduleJobs)
            {
                if (scheduleJob.Triggers != null && scheduleJob.Triggers.Any())
                {
                    var isNewJob = true;
                    foreach (var trigger in scheduleJob.Triggers)
                    {

                        if (isNewJob)
                        {
                            if (!(await _scheduler.CheckExists(scheduleJob.JobDetail.Key, cancellationToken)))
                            {
                                await _scheduler.ScheduleJob(scheduleJob.JobDetail, trigger, cancellationToken);
                            }
                        }
                        else
                        {
                            if (!(await _scheduler.CheckExists(trigger.Key, cancellationToken)))
                            {
                                await _scheduler.ScheduleJob(trigger, cancellationToken);
                            }
                        }
                        isNewJob = false;
                    }
                }
                else
                {
                    await _scheduler.AddJob(scheduleJob.JobDetail, true, cancellationToken);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler.IsStarted)
                await _scheduler.Shutdown(cancellationToken);
        }
    }
}
