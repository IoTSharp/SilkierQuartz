using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz.Plugins.RecentHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class HistoryController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var store = Scheduler.Context.GetExecutionHistoryStore();

            ViewBag.HistoryEnabled = store != null;

            if (store == null)
                return View(null);

            var history = await store.FilterLast(100);

            var list = new List<object>();

            foreach (var h in history.OrderByDescending(x => x.ActualFireTimeUtc))
            {
                string state = "Finished", icon = "check";
                var endTime = h.FinishedTimeUtc;

                if (h.Vetoed)
                {
                    state = "Vetoed";
                    icon = "ban";
                }
                else if (!string.IsNullOrEmpty(h.ExceptionMessage))
                {
                    state = "Failed";
                    icon = "close";
                }
                else if (h.FinishedTimeUtc == null)
                {
                    state = "Running";
                    icon = "play";
                    endTime = DateTime.UtcNow;
                }

                var jobKey = h.Job.Split('.');
                var triggerKey = h.Trigger.Split('.');

                list.Add(new
                {
                    Entity = h,

                    JobGroup = jobKey[0],
                    JobName = h.Job.Substring(jobKey[0].Length + 1),
                    TriggerGroup = triggerKey[0],
                    TriggerName = h.Trigger.Substring(triggerKey[0].Length + 1),

                    ScheduledFireTimeUtc = h.ScheduledFireTimeUtc?.ToDefaultFormat(),
                    ActualFireTimeUtc = h.ActualFireTimeUtc.ToDefaultFormat(),
                    FinishedTimeUtc = h.FinishedTimeUtc?.ToDefaultFormat(),
                    Duration = (endTime - h.ActualFireTimeUtc)?.ToString("hh\\:mm\\:ss"),
                    State = state,
                    StateIcon = icon
                });
            }

            return View(list);
        }
    }
}
