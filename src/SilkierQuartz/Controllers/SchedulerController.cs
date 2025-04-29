using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class SchedulerController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var histStore = Scheduler.Context.GetExecutionHistoryStore();
            var metadata = await Scheduler.GetMetaData();
            IReadOnlyCollection<JobKey> jobKeys = null;
            IReadOnlyCollection<TriggerKey> triggerKeys = null;
            if (!Scheduler.IsShutdown)
            {
                try
                {
                    jobKeys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                    triggerKeys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
                }
                catch (NotImplementedException) { }
            }
            var currentlyExecutingJobs = await Scheduler.GetCurrentlyExecutingJobs();
            IEnumerable<object> pausedJobGroups = null;
            IEnumerable<object> pausedTriggerGroups = null;
            IEnumerable<ExecutionHistoryEntry> execHistory = null;
            if (!Scheduler.IsShutdown)
            {
                try
                {
                    pausedJobGroups = await GetGroupPauseState(await Scheduler.GetJobGroupNames(), async x => await Scheduler.IsJobGroupPaused(x));
                }
                catch (NotImplementedException) { }

                try
                {
                    pausedTriggerGroups = await GetGroupPauseState(await Scheduler.GetTriggerGroupNames(), async x => await Scheduler.IsTriggerGroupPaused(x));
                }
                catch (NotImplementedException) { }
            }

            int? failedJobs = null;
            var executedJobs = metadata.NumberOfJobsExecuted;

            if (histStore != null)
            {
                execHistory = await histStore?.FilterLast(10);
                executedJobs = await histStore?.GetTotalJobsExecuted();
                failedJobs = await histStore?.GetTotalJobsFailed();
            }

            var histogram = execHistory.ToHistogram(detailed: true) ?? Histogram.CreateEmpty();

            histogram.BarWidth = 14;

            return View(new
            {
                History = histogram,
                MetaData = metadata,
                RunningSince = metadata.RunningSince?.UtcDateTime.ToDefaultFormat() ?? "N / A",
                UtcLabel = DateTimeSettings.UseLocalTime ? string.Empty : "UTC",
                Environment.MachineName,
                Application = Environment.CommandLine,
                JobsCount = jobKeys?.Count ?? 0,
                TriggerCount = triggerKeys?.Count ?? 0,
                ExecutingJobs = currentlyExecutingJobs.Count,
                ExecutedJobs = executedJobs,
                FailedJobs = failedJobs?.ToString(CultureInfo.InvariantCulture) ?? "N / A",
                JobGroups = pausedJobGroups,
                TriggerGroups = pausedTriggerGroups,
                HistoryEnabled = histStore != null,
                EnableEdit = EnableEdit
            });
        }

        async Task<IEnumerable<object>> GetGroupPauseState(IEnumerable<string> groups, Func<string, Task<bool>> func)
        {
            var result = new List<object>();

            foreach (var name in groups.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase))
                result.Add(new { Name = name, IsPaused = await func(name) });

            return result;
        }

        public class ActionArgs
        {
            public string Action { get; set; }
            public string Name { get; set; }
            public string Groups { get; set; } // trigger-groups | job-groups
        }

        [HttpPost, JsonErrorResponse]
        public async Task Action([FromBody] ActionArgs args)
        {
            switch (args.Action.ToLower())
            {
                case "shutdown":
                    await Scheduler.Shutdown();
                    break;
                case "standby":
                    await Scheduler.Standby();
                    break;
                case "start":
                    await Scheduler.Start();
                    break;
                case "pause":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        await Scheduler.PauseAll();
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                            await Scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name));
                        else if (args.Groups == "job-groups")
                            await Scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(args.Name));
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                case "resume":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        await Scheduler.ResumeAll();
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                            await Scheduler.ResumeTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name));
                        else if (args.Groups == "job-groups")
                            await Scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(args.Name));
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid action: " + args.Action);
            }
        }
    }
}
