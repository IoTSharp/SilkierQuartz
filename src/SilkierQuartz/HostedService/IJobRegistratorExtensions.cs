using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SilkierQuartz.HostedService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkierQuartz
{
    public static class IJobRegistratorExtensions
    {
        public static IJobRegistrator RegiserCRONJob<TJob>(
            this IJobRegistrator jobRegistrator,
            Action<JobOptions> jobOptions) where TJob : class, IJob
        {
            var options = new JobOptions();
            jobOptions?.Invoke(options);

            return jobRegistrator.RegiserJob<TJob>(
                options.Triggers?.Select(n => n.CreateTriggerBuilder())
                );
        }

        public static IJobRegistrator RegiserJob<TJob>(
            this IJobRegistrator jobRegistrator,
            Func<IEnumerable<TriggerBuilder>> triggers) where TJob : class, IJob
        {
            return jobRegistrator.RegiserJob<TJob>(triggers());
        }

        public static IServiceCollection AddQuartzJobDetail(this IServiceCollection services, Func<IJobDetail> detail)
        {
            services.AddSingleton<IScheduleJob>(provider => new ScheduleJob(detail(), new List<ITrigger>()));
            return services;
        }

        public static IServiceCollection AddQuartzJob<TJob>(this IServiceCollection services, string identity, bool durability = false, string? group = null, string? description = null) where TJob : class
        {
            return services.AddQuartzJob(typeof(TJob), identity, durability, group, description);
        }

        public static IServiceCollection AddQuartzJob(this IServiceCollection services, Type t, string identity, bool durability = false, string ? group = null, string? description = null)
        {
            if (!services.Any(sd => sd.ServiceType == t))
            {
                services.AddTransient(t);
            }
            var jobDetail = JobBuilder.Create(t).StoreDurably(durability).WithIdentity(identity, group).WithDescription(description).Build();
            services.AddSingleton<IScheduleJob>(provider => new ScheduleJob(jobDetail, new List<ITrigger>()));
            return services;
        }

        public static IServiceCollection AddQuartzJobDetail(this IServiceCollection services, IJobDetail detail)
        {
            services.AddSingleton<IScheduleJob>(provider => new ScheduleJob(detail, new List<ITrigger>()));
            return services;
        }

        public static IServiceCollection AddQuartzJob<TJob>(this IServiceCollection services) where TJob : class
        {
            services.AddTransient<TJob>();
            var jobDetail = JobBuilder.Create(typeof(TJob)).Build();
            services.AddSingleton<IScheduleJob>(provider => new ScheduleJob(jobDetail, new List<ITrigger>()));
            return services;
        }

        public static IApplicationBuilder UseQuartzJob<TJob>(
            this IApplicationBuilder app,
            Func<TriggerBuilder> triggerBuilder_func) where TJob : class, IJob
        {
            return app.UseQuartzJob<TJob>(new TriggerBuilder[] { triggerBuilder_func() });
        }

        public static IApplicationBuilder UseQuartzJob<TJob>(
            this IApplicationBuilder app, 
            string JobKey,
            Func<TriggerBuilder> triggerBuilder_func) where TJob : class, IJob
        {
            var _scheduleJobs = app.ApplicationServices.GetService<IEnumerable<IScheduleJob>>();

            var job = from js in _scheduleJobs where js.JobDetail.JobType == typeof(TJob) && js.JobDetail.Key.Name == JobKey select js;
            if (job.Any())
            {
                var scheduleJob = job.First();
                var lstgs = (List<ITrigger>)scheduleJob.Triggers;
                lstgs.Add(triggerBuilder_func().ForJob(scheduleJob.JobDetail).Build());
            }
            return app;
        }

        public static IApplicationBuilder UseQuartzJob<TJob>(
            this IApplicationBuilder app,
            TriggerBuilder triggerBuilder) where TJob : class, IJob
        {
            return app.UseQuartzJob<TJob>(new TriggerBuilder[] { triggerBuilder });
        }

        public static IApplicationBuilder UseQuartzJob<TJob>(
            this IApplicationBuilder app,
            Func<IEnumerable<TriggerBuilder>> triggerBuilders_func) where TJob : class, IJob
        {
            return app.UseQuartzJob<TJob>(triggerBuilders_func());
        }

        public static IApplicationBuilder UseQuartzJob<TJob>(
            this IApplicationBuilder app,
            IEnumerable<TriggerBuilder> triggerBuilders) where TJob : class, IJob
        {
            return app.UseQuartzJob(typeof(TJob), triggerBuilders);
        }

        public static IApplicationBuilder UseQuartzJob(
            this IApplicationBuilder app, 
            Type t,
            TriggerBuilder triggerBuilder)
        {
            return app.UseQuartzJob(t, new TriggerBuilder[] { triggerBuilder });
        }

        public static IApplicationBuilder UseQuartzJob(
            this IApplicationBuilder app, 
            Type t,
            Func<TriggerBuilder> triggerBuilders_func)
        {
            var lst = new List<TriggerBuilder>();
            var tb = triggerBuilders_func?.Invoke();
            if (tb != null)
            {
                lst.Add(tb);
            }
            return app.UseQuartzJob(t, lst);
        }

        public static IApplicationBuilder UseQuartzJob(
            this IApplicationBuilder app, 
            Type t,
            Func<IEnumerable<TriggerBuilder>> triggerBuilders_func)
        {
            return app.UseQuartzJob(t, triggerBuilders_func());
        }

        public static IApplicationBuilder UseQuartzJob(
            this IApplicationBuilder app, 
            Type t,
            IEnumerable<TriggerBuilder> triggerBuilders)
        {
            var _scheduleJobs = app.ApplicationServices.GetService<IEnumerable<IScheduleJob>>();
            var job = from js in _scheduleJobs where js.JobDetail.JobType == t select js;
            if (job.Any())
            {
                var scheduleJob = job.First();
                var lstgs = (List<ITrigger>)scheduleJob.Triggers;
                triggerBuilders.ToList().ForEach(triggerBuilder =>
                {
                    lstgs.Add(triggerBuilder.ForJob(scheduleJob.JobDetail).Build());
                });
               
            }
            return app;
        }

        public static IJobRegistrator RegiserJob<TJob>(
            this IJobRegistrator jobRegistrator,
            IEnumerable<TriggerBuilder> triggerBuilders)
            where TJob : class, IJob
        {
            jobRegistrator.Services.AddTransient<TJob>();

            var jobDetail = JobBuilder.Create<TJob>().Build();

            var triggers = new List<ITrigger>(triggerBuilders.Count());
            foreach (var triggerBuilder in triggerBuilders)
            {
                triggers.Add(triggerBuilder.ForJob(jobDetail).Build());
            }
            jobRegistrator.Services.AddSingleton<IScheduleJob>(provider => new ScheduleJob(jobDetail, triggers));

            return jobRegistrator;
        }
    }
}