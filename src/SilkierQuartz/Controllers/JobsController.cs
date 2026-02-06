using HandlebarsDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    internal static class JobControllerUtils
    {
        private const string USERNAME_KEY = "__USERNAME";

        public static ITrigger CreateAdHocTrigger(string username, JobKey jobKey, JobDataMap jobData)
        {
            var dt = DateTime.UtcNow;
            var truncatedUsername = username.Length > 30 ? username.Substring(0, 30) : username;
            var triggerName = $"{truncatedUsername}-{dt.ToString("ddMMHHmmss")}";
            if (!jobData.ContainsKey(USERNAME_KEY))
                jobData.Put(USERNAME_KEY, truncatedUsername);
            return
                TriggerBuilder.Create()
                    .WithIdentity(triggerName, "MT")
                    .UsingJobData(jobData)
                    .StartNow()
                    .ForJob(jobKey)
                    .Build();
        }
    }

    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class JobsController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var keys = (await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup())).OrderBy(x => x.ToString());
            var list = new List<JobListItem>();
            var knownTypes = new List<string>();

            foreach (var key in keys)
            {
                var detail = await GetJobDetail(key);
                var item = new JobListItem()
                {
                    Concurrent = !detail.ConcurrentExecutionDisallowed,
                    Persist = detail.PersistJobDataAfterExecution,
                    Recovery = detail.RequestsRecovery,
                    JobName = key.Name,
                    Group = key.Group,
                    Type = detail.JobType.FullName,
                    History = Histogram.Empty,
                    Description = detail.Description,
                };
                knownTypes.Add(detail.JobType.RemoveAssemblyDetails());
                list.Add(item);
            }

            Services.Cache.UpdateJobTypes(knownTypes);

            ViewBag.Groups = (await Scheduler.GetJobGroupNames()).GroupArray();

            ViewBag.EnableEdit = EnableEdit;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            var job = new JobPropertiesViewModel() { IsNew = true };
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            job.GroupList = (await Scheduler.GetJobGroupNames()).GroupArray();
            job.Group = SchedulerConstants.DefaultGroup;
            job.TypeList = Services.Cache.JobTypes;

            ViewBag.EnableEdit = EnableEdit;

            return View("Edit", new JobViewModel() { Job = job, DataMap = jobDataMap });
        }

        [HttpGet]
        public async Task<IActionResult> Trigger(string name, string group)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey);
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            ViewBag.JobName = name;
            ViewBag.Group = group;

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(Services));

            return View(jobDataMap);
        }

        [HttpPost, ActionName("Trigger"), JsonErrorResponse]
        public async Task<IActionResult> PostTrigger(string name, string group)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel(Services);

            var result = new ValidationResult();

            ModelValidator.Validate(jobDataMap, result.Errors);

            var username = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (result.Success)
            {
                var jobKey = JobKey.Create(name, group);
                var jobData = jobDataMap.GetQuartzJobDataMap();
                if (username != null)
                {
                    var trigger = JobControllerUtils.CreateAdHocTrigger(username, jobKey, jobData);
                    await Scheduler.ScheduleJob(trigger);
                }
                else
                {
                    await Scheduler.TriggerJob(jobKey, jobData);
                }
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name, string group, bool clone = false)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey);

            var jobModel = new JobPropertiesViewModel() { };
            var jobDataMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

            jobModel.IsNew = clone;
            jobModel.IsCopy = clone;
            jobModel.JobName = name;
            jobModel.Group = group;
            jobModel.GroupList = (await Scheduler.GetJobGroupNames()).GroupArray();

            jobModel.Type = job.JobType.RemoveAssemblyDetails();
            jobModel.TypeList = Services.Cache.JobTypes;

            jobModel.Description = job.Description;
            jobModel.Recovery = job.RequestsRecovery;
            jobModel.Concurrent = !job.ConcurrentExecutionDisallowed;
            jobModel.Persist = job.PersistJobDataAfterExecution;
            jobModel.Durable = job.Durable;

            if (clone)
                jobModel.JobName += " - Copy";

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(Services));

            ViewBag.EnableEdit = EnableEdit;

            return View("Edit", new JobViewModel() { Job = jobModel, DataMap = jobDataMap });
        }

        private async Task<IJobDetail> GetJobDetail(JobKey key)
        {
            var job = await Scheduler.GetJobDetail(key);

            if (job == null)
                throw new InvalidOperationException("Job " + key + " not found.");

            return job;
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromForm] JobViewModel model, bool trigger)
        {
            var jobModel = model.Job;
            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel(Services);

            var result = new ValidationResult();

            model.Validate(result.Errors);
            ModelValidator.Validate(jobDataMap, result.Errors);
            var jobData = jobDataMap.GetQuartzJobDataMap();

            if (result.Success)
            {
                IJobDetail BuildJob(JobBuilder builder)
                {
                    return builder
                        .OfType(Type.GetType(jobModel.Type, true))
                        .WithIdentity(jobModel.JobName, jobModel.Group)
                        .WithDescription(jobModel.Description)
                        .SetJobData(jobData)
                        .RequestRecovery(jobModel.Recovery)
                        .StoreDurably(jobModel.Durable)
                        .DisallowConcurrentExecution(!jobModel.Concurrent)
                        .PersistJobDataAfterExecution(jobModel.Persist)
                        .Build();
                }

                if (jobModel.IsNew)
                {
                    await Scheduler.AddJob(BuildJob(JobBuilder.Create().StoreDurably()), replace: false);
                }
                else
                {
                    var oldJob = await GetJobDetail(JobKey.Create(jobModel.OldJobName, jobModel.OldGroup));
                    await Scheduler.UpdateJob(oldJob.Key, BuildJob(oldJob.GetJobBuilder()));
                }

                if (trigger)
                {
                    var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var jobKey = JobKey.Create(jobModel.JobName, jobModel.Group);
                    if (username != null)
                    {
                        var jobTrigger = JobControllerUtils.CreateAdHocTrigger(username, jobKey, jobData);
                        await Scheduler.ScheduleJob(jobTrigger);
                    }
                    else
                    {
                        await Scheduler.TriggerJob(jobKey);
                    }
                }
            }

            return Json(result);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Delete([FromBody] KeyModel model)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var key = model.ToJobKey();

            if (!await Scheduler.DeleteJob(key))
                throw new InvalidOperationException("Cannot delete job " + key);

            return NoContent();
        }

        [HttpGet, JsonErrorResponse]
        public async Task<IActionResult> AdditionalData()
        {
            var keys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var hsl = Scheduler.Context.GetExecutionHistoryStore();
            var history = hsl != null ? await hsl.FilterLastOfEveryJob(10) : null;
            var historyByJob = history?.ToLookup(x => x.Job);

            var list = new List<object>();
            foreach (var key in keys)
            {
                var triggers = await Scheduler.GetTriggersOfJob(key);

                var nextFires = triggers.Select(x => x.GetNextFireTimeUtc()?.UtcDateTime).ToArray();

                list.Add(new
                {
                    JobName = key.Name,
                    key.Group,
                    History = historyByJob?.TryGet(key.ToString())?.ToHistogram(),
                    NextFireTime = nextFires.Where(x => x != null).OrderBy(x => x).FirstOrDefault()?.ToDefaultFormat(),
                });
            }

            return View(list);
        }

        [HttpGet]
        public Task<IActionResult> Duplicate(string name, string group)
        {
            return Edit(name, group, clone: true);
        }

        bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
        bool EnsureValidKey(KeyModel model) => EnsureValidKey(model.Name, model.Group);
    }
}
